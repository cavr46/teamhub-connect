using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamHubConnect.Application.Common.Interfaces;
using TeamHubConnect.Domain.Entities;

namespace TeamHubConnect.Application.Features.Messages.Commands.AddReaction;

public class AddReactionCommandHandler : IRequestHandler<AddReactionCommand, AddReactionResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRealtimeService _realtimeService;
    private readonly ILogger<AddReactionCommandHandler> _logger;

    public AddReactionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IRealtimeService realtimeService,
        ILogger<AddReactionCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task<AddReactionResult> Handle(AddReactionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new AddReactionResult(false, "User not authenticated");
            }

            var message = await _context.Messages
                .Include(m => m.Reactions)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == request.MessageId && !m.IsDeleted, cancellationToken);

            if (message == null)
            {
                return new AddReactionResult(false, "Message not found");
            }

            // Check if user already reacted with this emoji
            var existingReaction = message.Reactions
                .FirstOrDefault(r => r.UserId == currentUserId.Value && r.Emoji == request.Emoji);

            if (existingReaction != null)
            {
                // Remove existing reaction (toggle)
                _context.MessageReactions.Remove(existingReaction);
                _logger.LogInformation("Removed reaction {Emoji} from message {MessageId} by user {UserId}", 
                    request.Emoji, request.MessageId, currentUserId);
            }
            else
            {
                // Add new reaction
                var newReaction = new MessageReaction(
                    request.MessageId,
                    currentUserId.Value,
                    request.Emoji
                );
                await _context.MessageReactions.AddAsync(newReaction, cancellationToken);
                _logger.LogInformation("Added reaction {Emoji} to message {MessageId} by user {UserId}", 
                    request.Emoji, request.MessageId, currentUserId);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Reload to get updated reactions
            var updatedMessage = await _context.Messages
                .Include(m => m.Reactions)
                    .ThenInclude(r => r.User)
                .FirstAsync(m => m.Id == request.MessageId, cancellationToken);

            var emojiReactions = updatedMessage.Reactions
                .Where(r => r.Emoji == request.Emoji)
                .ToList();

            var reactionSummary = new ReactionSummary(
                request.Emoji,
                emojiReactions.Count,
                emojiReactions.Any(r => r.UserId == currentUserId.Value),
                emojiReactions.Select(r => new UserReactionDto(
                    r.UserId,
                    r.User.DisplayName,
                    r.User.Avatar))
                .ToList()
            );

            // Notify connected clients about the reaction change
            await _realtimeService.NotifyReactionChangedAsync(
                message.ChannelId, 
                request.MessageId, 
                reactionSummary, 
                cancellationToken);

            return new AddReactionResult(true, Reaction: reactionSummary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reaction {Emoji} to message {MessageId}", 
                request.Emoji, request.MessageId);
            return new AddReactionResult(false, "An error occurred while adding the reaction");
        }
    }
}