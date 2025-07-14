using TeamHubConnect.Domain.Common;

namespace TeamHubConnect.Domain.Entities;

public class MessageReaction : BaseEntity
{
    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }
    public string Emoji { get; private set; } = string.Empty;

    public virtual Message Message { get; private set; } = null!;
    public virtual User User { get; private set; } = null!;

    private MessageReaction() { }

    public MessageReaction(Guid messageId, Guid userId, string emoji)
    {
        MessageId = messageId;
        UserId = userId;
        Emoji = emoji;
    }

    public static MessageReaction Create(Guid messageId, string emoji, Guid userId)
    {
        return new MessageReaction(messageId, userId, emoji);
    }
}