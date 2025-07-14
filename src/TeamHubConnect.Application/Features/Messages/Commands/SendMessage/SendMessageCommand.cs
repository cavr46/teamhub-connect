using MediatR;
using TeamHubConnect.Domain.Enums;

namespace TeamHubConnect.Application.Features.Messages.Commands.SendMessage;

public record SendMessageCommand : IRequest<SendMessageResult>
{
    public string Content { get; init; } = null!;
    public Guid ChannelId { get; init; }
    public MessageType Type { get; init; } = MessageType.Text;
    public Guid? ParentMessageId { get; init; }
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;
    public DateTime? ScheduledAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public List<MessageAttachmentDto> Attachments { get; init; } = [];
}

public record MessageAttachmentDto
{
    public string Filename { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public long Size { get; init; }
    public string Url { get; init; } = null!;
    public string? ThumbnailUrl { get; init; }
}

public record SendMessageResult
{
    public Guid MessageId { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsScheduled { get; init; }
}