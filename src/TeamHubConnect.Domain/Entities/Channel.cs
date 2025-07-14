using TeamHubConnect.Domain.Common;
using TeamHubConnect.Domain.Enums;
using TeamHubConnect.Domain.ValueObjects;

namespace TeamHubConnect.Domain.Entities;

public class Channel : BaseAuditableEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Topic { get; private set; }
    public ChannelType Type { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime? ArchivedAt { get; private set; }
    public string? ArchivedBy { get; private set; }
    public string? ArchiveReason { get; private set; }
    public bool IsReadOnly { get; private set; }
    public int MaxMembers { get; private set; } = 10000;
    public int MessageRetentionDays { get; private set; } = 365;
    public bool AllowThreads { get; private set; } = true;
    public bool AllowReactions { get; private set; } = true;
    public bool AllowFileUploads { get; private set; } = true;
    public long MaxFileSize { get; private set; } = 50 * 1024 * 1024; // 50MB
    public ChannelSettings Settings { get; private set; } = new();

    public Workspace Workspace { get; private set; } = null!;

    private readonly List<ChannelMember> _members = [];
    public IReadOnlyCollection<ChannelMember> Members => _members.AsReadOnly();

    private readonly List<Message> _messages = [];
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    private readonly List<ChannelPin> _pinnedMessages = [];
    public IReadOnlyCollection<ChannelPin> PinnedMessages => _pinnedMessages.AsReadOnly();

    private Channel() { }

    public static Channel Create(
        string name,
        string? description,
        ChannelType type,
        Guid workspaceId,
        Guid? createdBy = null)
    {
        var channel = new Channel
        {
            Name = name,
            Description = description,
            Type = type,
            WorkspaceId = workspaceId
        };

        if (createdBy.HasValue)
        {
            channel.SetCreatedBy(createdBy.Value.ToString());
            channel.AddMember(createdBy.Value, ChannelRole.Admin);
        }

        return channel;
    }

    public void UpdateDetails(
        string? name = null,
        string? description = null,
        string? topic = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;
            
        Description = description;
        Topic = topic;
        
        MarkAsUpdated();
    }

    public void UpdateSettings(ChannelSettings settings)
    {
        Settings = settings;
        MarkAsUpdated();
    }

    public void UpdatePermissions(
        bool allowThreads = true,
        bool allowReactions = true,
        bool allowFileUploads = true,
        long maxFileSize = 50 * 1024 * 1024)
    {
        AllowThreads = allowThreads;
        AllowReactions = allowReactions;
        AllowFileUploads = allowFileUploads;
        MaxFileSize = maxFileSize;
        
        MarkAsUpdated();
    }

    public void SetMaxMembers(int maxMembers)
    {
        MaxMembers = maxMembers;
        MarkAsUpdated();
    }

    public void SetMessageRetention(int days)
    {
        MessageRetentionDays = days;
        MarkAsUpdated();
    }

    public void AddMember(Guid userId, ChannelRole role = ChannelRole.Member)
    {
        if (Type == ChannelType.DirectMessage)
            throw new InvalidOperationException("Cannot add members to direct message channels");

        if (_members.Count >= MaxMembers)
            throw new InvalidOperationException("Channel has reached maximum member limit");

        if (_members.Any(m => m.UserId == userId))
            return;

        _members.Add(ChannelMember.Create(Id, userId, role));
        MarkAsUpdated();
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            _members.Remove(member);
            MarkAsUpdated();
        }
    }

    public void UpdateMemberRole(Guid userId, ChannelRole role)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            member.UpdateRole(role);
            MarkAsUpdated();
        }
    }

    public Message PostMessage(
        string content,
        Guid authorId,
        MessageType type = MessageType.Text,
        Guid? parentMessageId = null)
    {
        if (IsArchived || IsReadOnly)
            throw new InvalidOperationException("Cannot post messages to archived or read-only channels");

        var message = Message.Create(content, authorId, Id, type, parentMessageId);
        _messages.Add(message);
        MarkAsUpdated();
        
        return message;
    }

    public void PinMessage(Guid messageId, Guid pinnedBy)
    {
        if (_pinnedMessages.Count >= 50) // Limit to 50 pinned messages
            throw new InvalidOperationException("Channel has reached maximum pinned message limit");

        if (_pinnedMessages.Any(p => p.MessageId == messageId))
            return;

        _pinnedMessages.Add(ChannelPin.Create(Id, messageId, pinnedBy));
        MarkAsUpdated();
    }

    public void UnpinMessage(Guid messageId)
    {
        var pin = _pinnedMessages.FirstOrDefault(p => p.MessageId == messageId);
        if (pin != null)
        {
            _pinnedMessages.Remove(pin);
            MarkAsUpdated();
        }
    }

    public void Archive(string reason, Guid archivedBy)
    {
        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
        ArchivedBy = archivedBy.ToString();
        ArchiveReason = reason;
        MarkAsUpdated();
    }

    public void Unarchive()
    {
        IsArchived = false;
        ArchivedAt = null;
        ArchivedBy = null;
        ArchiveReason = null;
        MarkAsUpdated();
    }

    public void SetReadOnly(bool readOnly)
    {
        IsReadOnly = readOnly;
        MarkAsUpdated();
    }

    public bool CanUserPost(Guid userId)
    {
        if (IsArchived || IsReadOnly)
            return false;

        if (Type == ChannelType.Public)
            return true;

        return _members.Any(m => m.UserId == userId && m.IsActive);
    }

    public bool CanUserRead(Guid userId)
    {
        if (Type == ChannelType.Public)
            return true;

        return _members.Any(m => m.UserId == userId && m.IsActive);
    }
}

public class ChannelMember : BaseEntity
{
    public Guid ChannelId { get; private set; }
    public Guid UserId { get; private set; }
    public ChannelRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LastReadAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsMuted { get; private set; }
    public DateTime? MutedUntil { get; private set; }

    public User User { get; private set; } = null!;
    public Channel Channel { get; private set; } = null!;

    private ChannelMember() { }

    public static ChannelMember Create(Guid channelId, Guid userId, ChannelRole role)
    {
        return new ChannelMember
        {
            ChannelId = channelId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void UpdateRole(ChannelRole role)
    {
        Role = role;
        MarkAsUpdated();
    }

    public void UpdateLastRead()
    {
        LastReadAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Mute(DateTime? until = null)
    {
        IsMuted = true;
        MutedUntil = until;
        MarkAsUpdated();
    }

    public void Unmute()
    {
        IsMuted = false;
        MutedUntil = null;
        MarkAsUpdated();
    }

    public void Leave()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void Rejoin()
    {
        IsActive = true;
        MarkAsUpdated();
    }
}

public class ChannelPin : BaseEntity
{
    public Guid ChannelId { get; private set; }
    public Guid MessageId { get; private set; }
    public Guid PinnedBy { get; private set; }
    public DateTime PinnedAt { get; private set; }

    public Channel Channel { get; private set; } = null!;
    public Message Message { get; private set; } = null!;

    private ChannelPin() { }

    public static ChannelPin Create(Guid channelId, Guid messageId, Guid pinnedBy)
    {
        return new ChannelPin
        {
            ChannelId = channelId,
            MessageId = messageId,
            PinnedBy = pinnedBy,
            PinnedAt = DateTime.UtcNow
        };
    }
}