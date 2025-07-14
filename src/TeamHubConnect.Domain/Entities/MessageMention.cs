using TeamHubConnect.Domain.Common;
using TeamHubConnect.Domain.Enums;

namespace TeamHubConnect.Domain.Entities;

public class MessageMention : BaseEntity
{
    public Guid MessageId { get; private set; }
    public Guid MentionedUserId { get; private set; }
    public MentionType Type { get; private set; }

    public virtual Message Message { get; private set; } = null!;
    public virtual User MentionedUser { get; private set; } = null!;

    private MessageMention() { }

    public MessageMention(Guid messageId, Guid mentionedUserId, MentionType type)
    {
        MessageId = messageId;
        MentionedUserId = mentionedUserId;
        Type = type;
    }
}