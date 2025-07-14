using TeamHubConnect.Domain.Common;
using TeamHubConnect.Domain.Enums;

namespace TeamHubConnect.Domain.Entities;

public class Message : BaseAuditableEntity
{
    public string Content { get; private set; } = null!;
    public string? FormattedContent { get; private set; }
    public MessageType Type { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid ChannelId { get; private set; }
    public Guid? ParentMessageId { get; private set; }
    public Guid? ThreadRootId { get; private set; }
    public bool IsEdited { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public string? EditReason { get; private set; }
    public new bool IsDeleted { get; private set; }
    public new DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }
    public MessagePriority Priority { get; private set; } = MessagePriority.Normal;
    public DateTime? ScheduledAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public MessageDeliveryStatus DeliveryStatus { get; private set; } = MessageDeliveryStatus.Sent;
    public int ViewCount { get; private set; }
    public MessageMetadata Metadata { get; private set; } = new();

    public User Author { get; private set; } = null!;
    public Channel Channel { get; private set; } = null!;
    public Message? ParentMessage { get; private set; }

    private readonly List<MessageReaction> _reactions = [];
    public IReadOnlyCollection<MessageReaction> Reactions => _reactions.AsReadOnly();

    private readonly List<MessageAttachment> _attachments = [];
    public IReadOnlyCollection<MessageAttachment> Attachments => _attachments.AsReadOnly();

    private readonly List<MessageMention> _mentions = [];
    public IReadOnlyCollection<MessageMention> Mentions => _mentions.AsReadOnly();

    private readonly List<MessageEdit> _editHistory = [];
    public IReadOnlyCollection<MessageEdit> EditHistory => _editHistory.AsReadOnly();

    private readonly List<Message> _replies = [];
    public IReadOnlyCollection<Message> Replies => _replies.AsReadOnly();

    private Message() { }

    public static Message Create(
        string content,
        Guid authorId,
        Guid channelId,
        MessageType type = MessageType.Text,
        Guid? parentMessageId = null)
    {
        var message = new Message
        {
            Content = content,
            Type = type,
            AuthorId = authorId,
            ChannelId = channelId,
            ParentMessageId = parentMessageId
        };

        if (parentMessageId.HasValue)
        {
            message.ThreadRootId = parentMessageId;
        }

        message.ProcessContent();
        return message;
    }

    public static Message CreateScheduled(
        string content,
        Guid authorId,
        Guid channelId,
        DateTime scheduledAt,
        MessageType type = MessageType.Text)
    {
        var message = new Message
        {
            Content = content,
            Type = type,
            AuthorId = authorId,
            ChannelId = channelId,
            ScheduledAt = scheduledAt,
            DeliveryStatus = MessageDeliveryStatus.Scheduled
        };

        message.ProcessContent();
        return message;
    }

    public void Edit(string newContent, string? reason = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot edit deleted message");

        var edit = new MessageEdit
        {
            PreviousContent = Content,
            NewContent = newContent,
            EditedAt = DateTime.UtcNow,
            Reason = reason
        };

        _editHistory.Add(edit);
        Content = newContent;
        IsEdited = true;
        EditedAt = DateTime.UtcNow;
        EditReason = reason;
        
        ProcessContent();
        MarkAsUpdated();
    }

    public void Delete(Guid deletedBy, bool soft = true)
    {
        if (soft)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedBy = deletedBy.ToString();
        }
        else
        {
            base.Delete();
        }
        
        MarkAsUpdated();
    }

    public new void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        MarkAsUpdated();
    }

    public void Edit(string newContent, string? editReason = null)
    {
        if (string.IsNullOrWhiteSpace(newContent))
            throw new ArgumentException("Message content cannot be empty", nameof(newContent));

        Content = newContent.Trim();
        IsEdited = true;
        EditedAt = DateTime.UtcNow;
        EditReason = editReason;
        MarkAsUpdated();
    }

    public void Delete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        MarkAsUpdated();
    }

    public void AddReaction(string emoji, Guid userId)
    {
        var existingReaction = _reactions.FirstOrDefault(r => 
            r.Emoji == emoji && r.UserId == userId);
            
        if (existingReaction != null)
            return;

        _reactions.Add(MessageReaction.Create(Id, emoji, userId));
        MarkAsUpdated();
    }

    public void RemoveReaction(string emoji, Guid userId)
    {
        var reaction = _reactions.FirstOrDefault(r => 
            r.Emoji == emoji && r.UserId == userId);
            
        if (reaction != null)
        {
            _reactions.Remove(reaction);
            MarkAsUpdated();
        }
    }

    public void AddAttachment(
        string filename,
        string contentType,
        long size,
        string url,
        string? thumbnailUrl = null)
    {
        _attachments.Add(MessageAttachment.Create(
            Id, filename, contentType, size, url, thumbnailUrl));
        MarkAsUpdated();
    }

    public void AddMention(Guid userId, MentionType type)
    {
        if (_mentions.Any(m => m.UserId == userId && m.Type == type))
            return;

        _mentions.Add(MessageMention.Create(Id, userId, type));
        MarkAsUpdated();
    }

    public void SetPriority(MessagePriority priority)
    {
        Priority = priority;
        MarkAsUpdated();
    }

    public void SetExpiration(DateTime expiresAt)
    {
        ExpiresAt = expiresAt;
        MarkAsUpdated();
    }

    public void IncrementViewCount()
    {
        ViewCount++;
        MarkAsUpdated();
    }

    public void UpdateDeliveryStatus(MessageDeliveryStatus status)
    {
        DeliveryStatus = status;
        MarkAsUpdated();
    }

    public void UpdateMetadata(MessageMetadata metadata)
    {
        Metadata = metadata;
        MarkAsUpdated();
    }

    private void ProcessContent()
    {
        FormattedContent = Content; // This would be processed by a markdown/formatting service
        
        // Extract mentions, hashtags, links, etc.
        // This is a simplified version - real implementation would use regex/parsers
        ExtractMentions();
    }

    private void ExtractMentions()
    {
        // Simple mention extraction - real implementation would be more sophisticated
        var words = Content.Split(' ');
        foreach (var word in words)
        {
            if (word.StartsWith("@"))
            {
                var username = word[1..];
                if (username == "channel")
                {
                    // Add channel mention
                }
                else if (username == "here")
                {
                    // Add here mention
                }
                else
                {
                    // Look up user by username and add mention
                }
            }
        }
    }

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    public bool IsScheduled => ScheduledAt.HasValue && ScheduledAt.Value > DateTime.UtcNow;
    public bool IsThreadReply => ParentMessageId.HasValue;
    public bool HasReplies => _replies.Any();
    public int ReplyCount => _replies.Count;
    public int ReactionCount => _reactions.Sum(r => r.Count);
}

public class MessageReaction : BaseEntity
{
    public Guid MessageId { get; private set; }
    public string Emoji { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public int Count { get; private set; } = 1;

    public Message Message { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private MessageReaction() { }

    public static MessageReaction Create(Guid messageId, string emoji, Guid userId)
    {
        return new MessageReaction
        {
            MessageId = messageId,
            Emoji = emoji,
            UserId = userId
        };
    }

    public void IncrementCount()
    {
        Count++;
        MarkAsUpdated();
    }

    public void DecrementCount()
    {
        if (Count > 0)
        {
            Count--;
            MarkAsUpdated();
        }
    }
}

public class MessageAttachment : BaseEntity
{
    public Guid MessageId { get; private set; }
    public string Filename { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long Size { get; private set; }
    public string Url { get; private set; } = null!;
    public string? ThumbnailUrl { get; private set; }
    public string? Description { get; private set; }
    public int DownloadCount { get; private set; }
    public bool IsScanned { get; private set; }
    public bool IsSafe { get; private set; } = true;

    public Message Message { get; private set; } = null!;

    private MessageAttachment() { }

    public static MessageAttachment Create(
        Guid messageId,
        string filename,
        string contentType,
        long size,
        string url,
        string? thumbnailUrl = null)
    {
        return new MessageAttachment
        {
            MessageId = messageId,
            Filename = filename,
            ContentType = contentType,
            Size = size,
            Url = url,
            ThumbnailUrl = thumbnailUrl
        };
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        MarkAsUpdated();
    }

    public void IncrementDownloadCount()
    {
        DownloadCount++;
        MarkAsUpdated();
    }

    public void MarkAsScanned(bool isSafe)
    {
        IsScanned = true;
        IsSafe = isSafe;
        MarkAsUpdated();
    }
}

public class MessageMention : BaseEntity
{
    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }
    public MentionType Type { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public Message Message { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private MessageMention() { }

    public static MessageMention Create(Guid messageId, Guid userId, MentionType type)
    {
        return new MessageMention
        {
            MessageId = messageId,
            UserId = userId,
            Type = type
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
}

public class MessageEdit
{
    public string PreviousContent { get; set; } = null!;
    public string NewContent { get; set; } = null!;
    public DateTime EditedAt { get; set; }
    public string? Reason { get; set; }
}

public class MessageMetadata
{
    public Dictionary<string, object> Properties { get; set; } = [];
    public string? SourceApplication { get; set; }
    public string? SourceVersion { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? ClientType { get; set; }
}