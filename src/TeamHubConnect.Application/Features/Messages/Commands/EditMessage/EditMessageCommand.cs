using MediatR;

namespace TeamHubConnect.Application.Features.Messages.Commands.EditMessage;

public record EditMessageCommand(
    Guid MessageId,
    string NewContent,
    string? EditReason = null
) : IRequest<EditMessageResult>;

public record EditMessageResult(
    bool Success,
    string? ErrorMessage = null,
    MessageDto? Message = null
);

public record MessageDto(
    Guid Id,
    string Content,
    Guid AuthorId,
    string AuthorName,
    Guid ChannelId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsEdited,
    DateTime? EditedAt,
    string? EditReason,
    List<ReactionDto> Reactions,
    List<AttachmentDto> Attachments
);

public record ReactionDto(
    string Emoji,
    int Count,
    List<Guid> UserIds
);

public record AttachmentDto(
    Guid Id,
    string FileName,
    string FileUrl,
    long FileSize,
    string ContentType
);