using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TeamHubConnect.Api.Hubs;

[Authorize]
public class PresenceHub : Hub
{
    private static readonly Dictionary<string, UserPresence> ConnectedUsers = new();

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(username))
        {
            ConnectedUsers[Context.ConnectionId] = new UserPresence
            {
                UserId = userId,
                Username = username,
                ConnectedAt = DateTime.UtcNow,
                Status = "Online"
            };

            await Clients.All.SendAsync("UserConnected", new { UserId = userId, Username = username, Status = "Online" });
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectedUsers.TryGetValue(Context.ConnectionId, out var user))
        {
            ConnectedUsers.Remove(Context.ConnectionId);
            
            // Check if user has other connections
            var hasOtherConnections = ConnectedUsers.Values.Any(u => u.UserId == user.UserId);
            
            if (!hasOtherConnections)
            {
                await Clients.All.SendAsync("UserDisconnected", new { UserId = user.UserId, Username = user.Username, Status = "Offline" });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task UpdateStatus(string status, string? message = null)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (!string.IsNullOrEmpty(userId) && ConnectedUsers.TryGetValue(Context.ConnectionId, out var user))
        {
            user.Status = status;
            user.StatusMessage = message;

            await Clients.All.SendAsync("UserStatusChanged", new 
            { 
                UserId = userId, 
                Username = username, 
                Status = status, 
                StatusMessage = message 
            });
        }
    }

    public async Task GetOnlineUsers()
    {
        var onlineUsers = ConnectedUsers.Values
            .GroupBy(u => u.UserId)
            .Select(g => g.First())
            .ToList();

        await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
    }
}

public class UserPresence
{
    public string UserId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public DateTime ConnectedAt { get; set; }
    public string Status { get; set; } = "Online";
    public string? StatusMessage { get; set; }
}