using Microsoft.AspNetCore.SignalR;
using TeamHubConnect.Application.Common.Interfaces;
using TeamHubConnect.Domain.Entities;
using TeamHubConnect.Api.Hubs;
using TeamHubConnect.Application.Features.Messages.Commands.EditMessage;
using TeamHubConnect.Application.Features.Messages.Commands.AddReaction;

namespace TeamHubConnect.Api.Services;

public class SignalRService : IRealtimeService
{
    private readonly IHubContext<ChatHub> _chatHubContext;
    private readonly IHubContext<PresenceHub> _presenceHubContext;

    public SignalRService(
        IHubContext<ChatHub> chatHubContext,
        IHubContext<PresenceHub> presenceHubContext)
    {
        _chatHubContext = chatHubContext;
        _presenceHubContext = presenceHubContext;
    }

    public async Task SendMessageToChannel(Guid channelId, Message message, CancellationToken cancellationToken = default)
    {
        var messageData = new
        {
            message.Id,
            message.Content,
            message.Type,
            message.AuthorId,
            message.ChannelId,
            message.CreatedAt,
            message.IsEdited,
            message.Priority
        };

        await _chatHubContext.Clients.Group($"channel_{channelId}")
            .SendAsync("MessageReceived", messageData, cancellationToken);
    }

    public async Task SendMessageToUser(Guid userId, object message, CancellationToken cancellationToken = default)
    {
        await _chatHubContext.Clients.Group($"user_{userId}")
            .SendAsync("UserMessage", message, cancellationToken);
    }

    public async Task SendMessageToWorkspace(Guid workspaceId, object message, CancellationToken cancellationToken = default)
    {
        await _chatHubContext.Clients.Group($"workspace_{workspaceId}")
            .SendAsync("WorkspaceMessage", message, cancellationToken);
    }

    public async Task NotifyUserStatusChange(Guid userId, string status, CancellationToken cancellationToken = default)
    {
        await _presenceHubContext.Clients.All
            .SendAsync("UserStatusChanged", new { UserId = userId, Status = status }, cancellationToken);
    }

    public async Task NotifyTypingIndicator(Guid channelId, Guid userId, bool isTyping, CancellationToken cancellationToken = default)
    {
        await _chatHubContext.Clients.Group($"channel_{channelId}")
            .SendAsync("TypingIndicator", new { UserId = userId, IsTyping = isTyping }, cancellationToken);
    }

    public async Task NotifyReactionAdded(Guid messageId, string emoji, Guid userId, CancellationToken cancellationToken = default)
    {
        await _chatHubContext.Clients.All
            .SendAsync("ReactionAdded", new { MessageId = messageId, Emoji = emoji, UserId = userId }, cancellationToken);
    }

    public async Task NotifyReactionRemoved(Guid messageId, string emoji, Guid userId, CancellationToken cancellationToken = default)
    {
        await _chatHubContext.Clients.All
            .SendAsync("ReactionRemoved", new { MessageId = messageId, Emoji = emoji, UserId = userId }, cancellationToken);
    }

    public async Task NotifyChannelCreated(Guid workspaceId, Channel channel, CancellationToken cancellationToken = default)
    {
        var channelData = new
        {
            channel.Id,
            channel.Name,
            channel.Description,
            channel.Type,
            channel.WorkspaceId,
            channel.CreatedAt
        };

        await _chatHubContext.Clients.Group($"workspace_{workspaceId}")
            .SendAsync("ChannelCreated", channelData, cancellationToken);
    }

    public async Task NotifyChannelUpdated(Guid workspaceId, Channel channel, CancellationToken cancellationToken = default)
    {
        var channelData = new
        {
            channel.Id,
            channel.Name,
            channel.Description,
            channel.Type,
            channel.WorkspaceId,
            UpdatedAt = channel.UpdatedAt
        };

        await _chatHubContext.Clients.Group($"workspace_{workspaceId}")
            .SendAsync("ChannelUpdated", channelData, cancellationToken);
    }

    public async Task NotifyUserJoinedChannel(Guid channelId, Guid userId, CancellationToken cancellationToken = default)
    {
        await _chatHubContext.Clients.Group($"channel_{channelId}")
            .SendAsync("UserJoinedChannel", new { ChannelId = channelId, UserId = userId }, cancellationToken);
    }

    public async Task NotifyUserLeftChannel(Guid channelId, Guid userId, CancellationToken cancellationToken = default)
    {
        await _chatHubContext.Clients.Group($"channel_{channelId}")
            .SendAsync("UserLeftChannel", new { ChannelId = channelId, UserId = userId }, cancellationToken);
    }

    public async Task NotifyMessageEditedAsync(Guid channelId, MessageDto message, CancellationToken cancellationToken = default)
    {
        await _chatHubContext.Clients.Group($"channel_{channelId}")
            .SendAsync("MessageEdited", message, cancellationToken);
    }

    public async Task NotifyMessageDeletedAsync(Guid channelId, Guid messageId, CancellationToken cancellationToken = default)
    {
        await _chatHubContext.Clients.Group($"channel_{channelId}")
            .SendAsync("MessageDeleted", new { MessageId = messageId, ChannelId = channelId }, cancellationToken);
    }

    public async Task NotifyReactionChangedAsync(Guid channelId, Guid messageId, ReactionSummary reaction, CancellationToken cancellationToken = default)
    {
        await _chatHubContext.Clients.Group($"channel_{channelId}")
            .SendAsync("ReactionChanged", new { MessageId = messageId, Reaction = reaction }, cancellationToken);
    }
}