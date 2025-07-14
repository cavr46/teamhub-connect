using Microsoft.AspNetCore.SignalR.Client;
using Blazored.LocalStorage;

namespace TeamHubConnect.Blazor.Services;

public class SignalRService : ISignalRService, IAsyncDisposable
{
    private HubConnection? _chatConnection;
    private HubConnection? _presenceConnection;
    private readonly ILocalStorageService _localStorage;
    private readonly string _baseUrl;

    public SignalRService(ILocalStorageService localStorage, IConfiguration configuration)
    {
        _localStorage = localStorage;
        _baseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7001";
    }

    public bool IsConnected => 
        (_chatConnection?.State == HubConnectionState.Connected) &&
        (_presenceConnection?.State == HubConnectionState.Connected);

    // Events
    public event Func<object, Task> OnMessageReceived = delegate { return Task.CompletedTask; };
    public event Func<object, Task> OnUserStatusChanged = delegate { return Task.CompletedTask; };
    public event Func<object, Task> OnTypingIndicator = delegate { return Task.CompletedTask; };
    public event Func<object, Task> OnReactionAdded = delegate { return Task.CompletedTask; };
    public event Func<object, Task> OnReactionRemoved = delegate { return Task.CompletedTask; };
    public event Func<object, Task> OnChannelCreated = delegate { return Task.CompletedTask; };
    public event Func<object, Task> OnChannelUpdated = delegate { return Task.CompletedTask; };
    public event Func<object, Task> OnUserJoinedChannel = delegate { return Task.CompletedTask; };
    public event Func<object, Task> OnUserLeftChannel = delegate { return Task.CompletedTask; };
    public event Func<Task> OnConnected = delegate { return Task.CompletedTask; };
    public event Func<Exception?, Task> OnDisconnected = delegate { return Task.CompletedTask; };
    public event Func<string?, Task> OnReconnecting = delegate { return Task.CompletedTask; };
    public event Func<string?, Task> OnReconnected = delegate { return Task.CompletedTask; };

    public async Task StartAsync()
    {
        if (_chatConnection != null && _presenceConnection != null)
            return;

        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (string.IsNullOrEmpty(token))
            return;

        // Chat Hub Connection
        _chatConnection = new HubConnectionBuilder()
            .WithUrl($"{_baseUrl}/hub/chat", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .WithAutomaticReconnect()
            .Build();

        // Presence Hub Connection
        _presenceConnection = new HubConnectionBuilder()
            .WithUrl($"{_baseUrl}/hub/presence", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .WithAutomaticReconnect()
            .Build();

        // Setup event handlers
        SetupChatHubHandlers();
        SetupPresenceHubHandlers();
        SetupConnectionHandlers();

        // Start connections
        await _chatConnection.StartAsync();
        await _presenceConnection.StartAsync();
    }

    public async Task StopAsync()
    {
        if (_chatConnection != null)
        {
            await _chatConnection.StopAsync();
            await _chatConnection.DisposeAsync();
            _chatConnection = null;
        }

        if (_presenceConnection != null)
        {
            await _presenceConnection.StopAsync();
            await _presenceConnection.DisposeAsync();
            _presenceConnection = null;
        }
    }

    public async Task JoinWorkspaceAsync(Guid workspaceId)
    {
        if (_chatConnection?.State == HubConnectionState.Connected)
        {
            await _chatConnection.InvokeAsync("JoinWorkspace", workspaceId.ToString());
        }
    }

    public async Task LeaveWorkspaceAsync(Guid workspaceId)
    {
        if (_chatConnection?.State == HubConnectionState.Connected)
        {
            await _chatConnection.InvokeAsync("LeaveWorkspace", workspaceId.ToString());
        }
    }

    public async Task JoinChannelAsync(Guid channelId)
    {
        if (_chatConnection?.State == HubConnectionState.Connected)
        {
            await _chatConnection.InvokeAsync("JoinChannel", channelId.ToString());
        }
    }

    public async Task LeaveChannelAsync(Guid channelId)
    {
        if (_chatConnection?.State == HubConnectionState.Connected)
        {
            await _chatConnection.InvokeAsync("LeaveChannel", channelId.ToString());
        }
    }

    public async Task SendTypingIndicatorAsync(Guid channelId, bool isTyping)
    {
        if (_chatConnection?.State == HubConnectionState.Connected)
        {
            if (isTyping)
                await _chatConnection.InvokeAsync("StartTyping", channelId.ToString());
            else
                await _chatConnection.InvokeAsync("StopTyping", channelId.ToString());
        }
    }

    private void SetupChatHubHandlers()
    {
        if (_chatConnection == null) return;

        _chatConnection.On<object>("MessageReceived", async (message) => await OnMessageReceived.Invoke(message));
        _chatConnection.On<object>("TypingIndicator", async (indicator) => await OnTypingIndicator.Invoke(indicator));
        _chatConnection.On<object>("ReactionAdded", async (reaction) => await OnReactionAdded.Invoke(reaction));
        _chatConnection.On<object>("ReactionRemoved", async (reaction) => await OnReactionRemoved.Invoke(reaction));
        _chatConnection.On<object>("ChannelCreated", async (channel) => await OnChannelCreated.Invoke(channel));
        _chatConnection.On<object>("ChannelUpdated", async (channel) => await OnChannelUpdated.Invoke(channel));
        _chatConnection.On<object>("UserJoinedChannel", async (data) => await OnUserJoinedChannel.Invoke(data));
        _chatConnection.On<object>("UserLeftChannel", async (data) => await OnUserLeftChannel.Invoke(data));
    }

    private void SetupPresenceHubHandlers()
    {
        if (_presenceConnection == null) return;

        _presenceConnection.On<object>("UserStatusChanged", async (status) => await OnUserStatusChanged.Invoke(status));
        _presenceConnection.On<object>("UserConnected", async (user) => await OnUserStatusChanged.Invoke(user));
        _presenceConnection.On<object>("UserDisconnected", async (user) => await OnUserStatusChanged.Invoke(user));
    }

    private void SetupConnectionHandlers()
    {
        if (_chatConnection != null)
        {
            _chatConnection.Closed += async (error) => await OnDisconnected.Invoke(error);
            _chatConnection.Reconnecting += async (error) => await OnReconnecting.Invoke(error);
            _chatConnection.Reconnected += async (connectionId) => await OnReconnected.Invoke(connectionId);
        }

        if (_presenceConnection != null)
        {
            _presenceConnection.Closed += async (error) => await OnDisconnected.Invoke(error);
            _presenceConnection.Reconnecting += async (error) => await OnReconnecting.Invoke(error);
            _presenceConnection.Reconnected += async (connectionId) => await OnReconnected.Invoke(connectionId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}