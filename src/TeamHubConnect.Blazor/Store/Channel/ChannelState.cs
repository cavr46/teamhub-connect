using Fluxor;

namespace TeamHubConnect.Blazor.Store.Channel;

[FeatureState]
public record ChannelState
{
    public bool IsLoading { get; init; } = false;
    public List<ChannelDto> Channels { get; init; } = [];
    public Guid? CurrentChannelId { get; init; }
    public ChannelDto? CurrentChannel { get; init; }
    public string? ErrorMessage { get; init; }
}

public record ChannelDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string? Topic { get; init; }
    public string Type { get; init; } = "Public";
    public Guid WorkspaceId { get; init; }
    public bool IsArchived { get; init; }
    public bool IsPrivate { get; init; }
    public int MemberCount { get; init; }
    public bool HasUnread { get; init; }
    public int UnreadCount { get; init; }
    public bool IsMuted { get; init; }
    public DateTime? LastActivity { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<ChannelMemberDto> Members { get; init; } = [];
}

public record ChannelMemberDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? AvatarUrl { get; init; }
    public string Role { get; init; } = "Member";
    public DateTime JoinedAt { get; init; }
    public DateTime? LastReadAt { get; init; }
    public bool IsActive { get; init; } = true;
}

// Actions
public record LoadChannelsAction(Guid WorkspaceId);
public record LoadChannelsSuccessAction(List<ChannelDto> Channels);
public record LoadChannelsFailureAction(string ErrorMessage);
public record SelectChannelAction(Guid ChannelId);
public record SelectChannelSuccessAction(ChannelDto Channel);
public record CreateChannelAction(string Name, string? Description = null, string Type = "Public", bool IsPrivate = false);
public record CreateChannelSuccessAction(ChannelDto Channel);
public record CreateChannelFailureAction(string ErrorMessage);
public record UpdateChannelAction(Guid ChannelId, string? Name = null, string? Description = null, string? Topic = null);
public record JoinChannelAction(Guid ChannelId);
public record LeaveChannelAction(Guid ChannelId);
public record ArchiveChannelAction(Guid ChannelId, string Reason);
public record MuteChannelAction(Guid ChannelId, bool IsMuted);
public record MarkChannelAsReadAction(Guid ChannelId);
public record UpdateUnreadCountAction(Guid ChannelId, int Count);
public record ChannelUpdatedAction(ChannelDto Channel);
public record ChannelMemberJoinedAction(Guid ChannelId, ChannelMemberDto Member);
public record ChannelMemberLeftAction(Guid ChannelId, Guid UserId);