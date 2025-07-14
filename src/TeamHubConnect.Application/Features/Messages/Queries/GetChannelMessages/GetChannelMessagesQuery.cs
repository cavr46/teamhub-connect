using MediatR;

namespace TeamHubConnect.Application.Features.Messages.Queries.GetChannelMessages;

public record GetChannelMessagesQuery : IRequest<GetChannelMessagesResult>
{
    public Guid ChannelId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public DateTime? Before { get; init; }
    public DateTime? After { get; init; }
    public Guid? ThreadRootId { get; init; }
    public bool IncludeThreads { get; init; } = true;
}

public record GetChannelMessagesResult
{
    public List<MessageDto> Messages { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public bool HasMore { get; init; }
}

public record MessageDto
{
    public Guid Id { get; init; }
    public string Content { get; init; } = null!;
    public string? FormattedContent { get; init; }
    public string Type { get; init; } = null!;
    public Guid AuthorId { get; init; }
    public string AuthorName { get; init; } = null!;
    public string? AuthorAvatarUrl { get; init; }
    public Guid ChannelId { get; init; }
    public Guid? ParentMessageId { get; init; }
    public Guid? ThreadRootId { get; init; }
    public bool IsEdited { get; init; }
    public DateTime? EditedAt { get; init; }
    public string Priority { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsExpired { get; init; }
    public int ViewCount { get; init; }
    public int ReplyCount { get; init; }
    public List<MessageReactionDto> Reactions { get; init; } = [];
    public List<MessageAttachmentDto> Attachments { get; init; } = [];
    public List<MessageMentionDto> Mentions { get; init; } = [];
}

public record MessageReactionDto
{
    public string Emoji { get; init; } = null!;
    public int Count { get; init; }
    public List<Guid> UserIds { get; init; } = [];
    public bool UserReacted { get; init; }
}

public record MessageMentionDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = null!;
    public string Type { get; init; } = null!;
}