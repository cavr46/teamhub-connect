using TeamHubConnect.Domain.Entities;
using TeamHubConnect.Application.Features.Messages.Commands.EditMessage;
using TeamHubConnect.Application.Features.Messages.Commands.AddReaction;

namespace TeamHubConnect.Application.Common.Interfaces;

public interface IRealtimeService
{
    Task SendMessageToChannel(Guid channelId, Message message, CancellationToken cancellationToken = default);
    Task SendMessageToUser(Guid userId, object message, CancellationToken cancellationToken = default);
    Task SendMessageToWorkspace(Guid workspaceId, object message, CancellationToken cancellationToken = default);
    Task NotifyUserStatusChange(Guid userId, string status, CancellationToken cancellationToken = default);
    Task NotifyTypingIndicator(Guid channelId, Guid userId, bool isTyping, CancellationToken cancellationToken = default);
    Task NotifyReactionAdded(Guid messageId, string emoji, Guid userId, CancellationToken cancellationToken = default);
    Task NotifyReactionRemoved(Guid messageId, string emoji, Guid userId, CancellationToken cancellationToken = default);
    Task NotifyChannelCreated(Guid workspaceId, Channel channel, CancellationToken cancellationToken = default);
    Task NotifyChannelUpdated(Guid workspaceId, Channel channel, CancellationToken cancellationToken = default);
    Task NotifyUserJoinedChannel(Guid channelId, Guid userId, CancellationToken cancellationToken = default);
    Task NotifyUserLeftChannel(Guid channelId, Guid userId, CancellationToken cancellationToken = default);
    
    // New methods for message operations
    Task NotifyMessageEditedAsync(Guid channelId, MessageDto message, CancellationToken cancellationToken = default);
    Task NotifyMessageDeletedAsync(Guid channelId, Guid messageId, CancellationToken cancellationToken = default);
    Task NotifyReactionChangedAsync(Guid channelId, Guid messageId, ReactionSummary reaction, CancellationToken cancellationToken = default);
}