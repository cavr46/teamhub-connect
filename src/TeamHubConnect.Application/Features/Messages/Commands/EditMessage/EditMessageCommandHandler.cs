using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamHubConnect.Application.Common.Interfaces;

namespace TeamHubConnect.Application.Features.Messages.Commands.EditMessage;

public class EditMessageCommandHandler : IRequestHandler<EditMessageCommand, EditMessageResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRealtimeService _realtimeService;
    private readonly ILogger<EditMessageCommandHandler> _logger;

    public EditMessageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IRealtimeService realtimeService,
        ILogger<EditMessageCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task<EditMessageResult> Handle(EditMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new EditMessageResult(false, "User not authenticated");
            }

            var message = await _context.Messages
                .Include(m => m.Author)
                .Include(m => m.Channel)
                .Include(m => m.Reactions)
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.Id == request.MessageId && !m.IsDeleted, cancellationToken);

            if (message == null)
            {
                return new EditMessageResult(false, "Message not found");
            }

            // Check if user can edit the message (author or admin)
            if (message.AuthorId != currentUserId.Value)
            {
                // TODO: Check if user is admin/moderator
                return new EditMessageResult(false, "You can only edit your own messages");
            }

            // Check if message can be edited (within time limit, not system message, etc.)
            var editTimeLimit = TimeSpan.FromMinutes(30); // Configurable
            if (DateTime.UtcNow - message.CreatedAt > editTimeLimit)
            {
                return new EditMessageResult(false, "Message edit time limit exceeded");
            }

            message.Edit(request.NewContent, request.EditReason);
            await _context.SaveChangesAsync(cancellationToken);

            var messageDto = new MessageDto(
                message.Id,
                message.Content,
                message.AuthorId,
                message.Author.DisplayName,
                message.ChannelId,
                message.CreatedAt,
                message.UpdatedAt,
                message.IsEdited,
                message.EditedAt,
                message.EditReason,
                message.Reactions.GroupBy(r => r.Emoji)
                    .Select(g => new ReactionDto(
                        g.Key,
                        g.Count(),
                        g.Select(r => r.UserId).ToList()))
                    .ToList(),
                message.Attachments.Select(a => new AttachmentDto(
                    a.Id,
                    a.FileName,
                    a.FileUrl,
                    a.FileSize,
                    a.ContentType))
                    .ToList()
            );

            // Notify connected clients about the message edit
            await _realtimeService.NotifyMessageEditedAsync(message.ChannelId, messageDto, cancellationToken);

            _logger.LogInformation("Message {MessageId} edited by user {UserId}", request.MessageId, currentUserId);

            return new EditMessageResult(true, Message: messageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message {MessageId}", request.MessageId);
            return new EditMessageResult(false, "An error occurred while editing the message");
        }
    }
}