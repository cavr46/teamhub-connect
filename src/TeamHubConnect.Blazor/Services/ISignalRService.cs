using Microsoft.AspNetCore.SignalR.Client;

namespace TeamHubConnect.Blazor.Services;

public interface ISignalRService
{
    Task StartAsync();
    Task StopAsync();
    bool IsConnected { get; }
    
    // Connection Management
    Task JoinWorkspaceAsync(Guid workspaceId);
    Task LeaveWorkspaceAsync(Guid workspaceId);
    Task JoinChannelAsync(Guid channelId);
    Task LeaveChannelAsync(Guid channelId);
    
    // Messaging
    Task SendTypingIndicatorAsync(Guid channelId, bool isTyping);
    
    // Events
    event Func<object, Task> OnMessageReceived;
    event Func<object, Task> OnUserStatusChanged;
    event Func<object, Task> OnTypingIndicator;
    event Func<object, Task> OnReactionAdded;
    event Func<object, Task> OnReactionRemoved;
    event Func<object, Task> OnChannelCreated;
    event Func<object, Task> OnChannelUpdated;
    event Func<object, Task> OnUserJoinedChannel;
    event Func<object, Task> OnUserLeftChannel;
    event Func<Task> OnConnected;
    event Func<Exception?, Task> OnDisconnected;
    event Func<string?, Task> OnReconnecting;
    event Func<string?, Task> OnReconnected;
}