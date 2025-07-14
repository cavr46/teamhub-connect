using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamHubConnect.Application.Common.Interfaces;

namespace TeamHubConnect.Application.Features.Messages.Commands.DeleteMessage;

public class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, DeleteMessageResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRealtimeService _realtimeService;
    private readonly ILogger<DeleteMessageCommandHandler> _logger;

    public DeleteMessageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IRealtimeService realtimeService,
        ILogger<DeleteMessageCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task<DeleteMessageResult> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return new DeleteMessageResult(false, "User not authenticated");
            }

            var message = await _context.Messages
                .Include(m => m.Channel)
                .FirstOrDefaultAsync(m => m.Id == request.MessageId && !m.IsDeleted, cancellationToken);

            if (message == null)
            {
                return new DeleteMessageResult(false, "Message not found");
            }

            // Check if user can delete the message (author or admin)
            var canDelete = message.AuthorId == currentUserId.Value;
            
            if (!canDelete)
            {
                // TODO: Check if user is admin/moderator of the channel
                return new DeleteMessageResult(false, "You can only delete your own messages");
            }

            if (request.HardDelete)
            {
                // Hard delete - completely remove from database
                _context.Messages.Remove(message);
            }
            else
            {
                // Soft delete - mark as deleted
                message.Delete(currentUserId.Value.ToString());
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Notify connected clients about the message deletion
            await _realtimeService.NotifyMessageDeletedAsync(message.ChannelId, request.MessageId, cancellationToken);

            _logger.LogInformation("Message {MessageId} {DeleteType} deleted by user {UserId}", 
                request.MessageId, 
                request.HardDelete ? "hard" : "soft", 
                currentUserId);

            return new DeleteMessageResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", request.MessageId);
            return new DeleteMessageResult(false, "An error occurred while deleting the message");
        }
    }
}