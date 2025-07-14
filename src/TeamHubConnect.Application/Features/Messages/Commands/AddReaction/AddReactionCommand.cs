using MediatR;

namespace TeamHubConnect.Application.Features.Messages.Commands.AddReaction;

public record AddReactionCommand(
    Guid MessageId,
    string Emoji
) : IRequest<AddReactionResult>;

public record AddReactionResult(
    bool Success,
    string? ErrorMessage = null,
    ReactionSummary? Reaction = null
);

public record ReactionSummary(
    string Emoji,
    int Count,
    bool UserReacted,
    List<UserReactionDto> Users
);

public record UserReactionDto(
    Guid UserId,
    string DisplayName,
    string? Avatar
);