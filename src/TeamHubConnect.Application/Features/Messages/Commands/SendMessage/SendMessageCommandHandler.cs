using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamHubConnect.Application.Common.Interfaces;
using TeamHubConnect.Domain.Entities;

namespace TeamHubConnect.Application.Features.Messages.Commands.SendMessage;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRealtimeService _realtimeService;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IRealtimeService realtimeService,
        ILogger<SendMessageCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task<SendMessageResult> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("User must be authenticated");

        // Verify channel exists and user has permission
        var channel = await _context.Channels
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, cancellationToken);

        if (channel == null)
            throw new InvalidOperationException("Channel not found");

        if (!channel.CanUserPost(userId.Value))
            throw new UnauthorizedAccessException("User does not have permission to post in this channel");

        // Create message
        var message = request.ScheduledAt.HasValue
            ? Message.CreateScheduled(request.Content, userId.Value, request.ChannelId, request.ScheduledAt.Value, request.Type)
            : Message.Create(request.Content, userId.Value, request.ChannelId, request.Type, request.ParentMessageId);

        message.SetPriority(request.Priority);

        if (request.ExpiresAt.HasValue)
            message.SetExpiration(request.ExpiresAt.Value);

        // Add attachments
        foreach (var attachment in request.Attachments)
        {
            message.AddAttachment(
                attachment.Filename,
                attachment.ContentType,
                attachment.Size,
                attachment.Url,
                attachment.ThumbnailUrl);
        }

        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Message {MessageId} sent by user {UserId} to channel {ChannelId}", 
            message.Id, userId.Value, request.ChannelId);

        // Send real-time notification if not scheduled
        if (!request.ScheduledAt.HasValue || request.ScheduledAt.Value <= DateTime.UtcNow)
        {
            await _realtimeService.SendMessageToChannel(request.ChannelId, message, cancellationToken);
        }

        return new SendMessageResult
        {
            MessageId = message.Id,
            CreatedAt = message.CreatedAt,
            IsScheduled = message.IsScheduled
        };
    }
}