using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamHubConnect.Application.Common.Interfaces;
using TeamHubConnect.Domain.Entities;
using TeamHubConnect.Domain.Enums;

namespace TeamHubConnect.Application.Features.Channels.Commands.CreateChannel;

public class CreateChannelCommandHandler : IRequestHandler<CreateChannelCommand, CreateChannelResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRealtimeService _realtimeService;
    private readonly ILogger<CreateChannelCommandHandler> _logger;

    public CreateChannelCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IRealtimeService realtimeService,
        ILogger<CreateChannelCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task<CreateChannelResult> Handle(CreateChannelCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("User must be authenticated");

        // Verify workspace exists and user has permission
        var workspace = await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId, cancellationToken);

        if (workspace == null)
            throw new InvalidOperationException("Workspace not found");

        var userMembership = workspace.Members.FirstOrDefault(m => m.UserId == userId.Value);
        if (userMembership == null || !userMembership.IsActive)
            throw new UnauthorizedAccessException("User is not a member of this workspace");

        // Check if user can create channels
        if (userMembership.Role == WorkspaceRole.Guest)
            throw new UnauthorizedAccessException("Guests cannot create channels");

        // Check workspace limits
        var channelCount = await _context.Channels
            .CountAsync(c => c.WorkspaceId == request.WorkspaceId && !c.IsDeleted, cancellationToken);
            
        if (channelCount >= workspace.MaxChannels)
            throw new InvalidOperationException("Workspace has reached maximum channel limit");

        // Check if channel name already exists
        var existingChannel = await _context.Channels
            .FirstOrDefaultAsync(c => c.WorkspaceId == request.WorkspaceId && 
                                    c.Name.ToLower() == request.Name.ToLower() && 
                                    !c.IsDeleted, cancellationToken);

        if (existingChannel != null)
            throw new InvalidOperationException("A channel with this name already exists");

        // Create channel
        var channelType = request.IsPrivate ? ChannelType.Private : request.Type;
        var channel = Channel.Create(request.Name, request.Description, channelType, request.WorkspaceId, userId.Value);
        
        channel.UpdateDetails(topic: request.Topic);
        channel.SetMaxMembers(request.MaxMembers);
        channel.UpdatePermissions(request.AllowThreads, request.AllowReactions, request.AllowFileUploads);

        _context.Channels.Add(channel);

        // Add specified members
        foreach (var memberId in request.MemberIds)
        {
            var member = workspace.Members.FirstOrDefault(m => m.UserId == memberId && m.IsActive);
            if (member != null)
            {
                channel.AddMember(memberId, ChannelRole.Member);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Channel {ChannelId} created by user {UserId} in workspace {WorkspaceId}", 
            channel.Id, userId.Value, request.WorkspaceId);

        // Send real-time notification
        await _realtimeService.NotifyChannelCreated(request.WorkspaceId, channel, cancellationToken);

        return new CreateChannelResult
        {
            ChannelId = channel.Id,
            Name = channel.Name,
            Type = channel.Type.ToString(),
            CreatedAt = channel.CreatedAt
        };
    }
}