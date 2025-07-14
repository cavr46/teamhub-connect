using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamHubConnect.Application.Common.Interfaces;
using TeamHubConnect.Domain.Enums;

namespace TeamHubConnect.Application.Features.Messages.Queries.GetChannelMessages;

public class GetChannelMessagesQueryHandler : IRequestHandler<GetChannelMessagesQuery, GetChannelMessagesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetChannelMessagesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetChannelMessagesResult> Handle(GetChannelMessagesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("User must be authenticated");

        // Verify channel exists and user has permission
        var channel = await _context.Channels
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, cancellationToken);

        if (channel == null)
            throw new InvalidOperationException("Channel not found");

        if (!channel.CanUserRead(userId.Value))
            throw new UnauthorizedAccessException("User does not have permission to read this channel");

        var query = _context.Messages
            .Include(m => m.Author)
            .Include(m => m.Reactions)
            .Include(m => m.Attachments)
            .Include(m => m.Mentions)
            .Where(m => m.ChannelId == request.ChannelId && !m.IsDeleted);

        // Filter by thread if specified
        if (request.ThreadRootId.HasValue)
        {
            query = query.Where(m => m.ThreadRootId == request.ThreadRootId.Value);
        }
        else if (!request.IncludeThreads)
        {
            query = query.Where(m => m.ParentMessageId == null);
        }

        // Apply date filters
        if (request.Before.HasValue)
        {
            query = query.Where(m => m.CreatedAt < request.Before.Value);
        }

        if (request.After.HasValue)
        {
            query = query.Where(m => m.CreatedAt > request.After.Value);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                Content = m.Content,
                FormattedContent = m.FormattedContent,
                Type = m.Type.ToString(),
                AuthorId = m.AuthorId,
                AuthorName = m.Author.DisplayName,
                AuthorAvatarUrl = m.Author.AvatarUrl,
                ChannelId = m.ChannelId,
                ParentMessageId = m.ParentMessageId,
                ThreadRootId = m.ThreadRootId,
                IsEdited = m.IsEdited,
                EditedAt = m.EditedAt,
                Priority = m.Priority.ToString(),
                CreatedAt = m.CreatedAt,
                ExpiresAt = m.ExpiresAt,
                IsExpired = m.IsExpired,
                ViewCount = m.ViewCount,
                ReplyCount = m.ReplyCount,
                Reactions = m.Reactions.GroupBy(r => r.Emoji).Select(g => new MessageReactionDto
                {
                    Emoji = g.Key,
                    Count = g.Sum(r => r.Count),
                    UserIds = g.Select(r => r.UserId).ToList(),
                    UserReacted = g.Any(r => r.UserId == userId.Value)
                }).ToList(),
                Attachments = m.Attachments.Select(a => new MessageAttachmentDto
                {
                    Filename = a.Filename,
                    ContentType = a.ContentType,
                    Size = a.Size,
                    Url = a.Url,
                    ThumbnailUrl = a.ThumbnailUrl
                }).ToList(),
                Mentions = m.Mentions.Select(mention => new MessageMentionDto
                {
                    UserId = mention.UserId,
                    Username = mention.User.Username,
                    Type = mention.Type.ToString()
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        // Reverse to show oldest first
        messages.Reverse();

        var hasMore = totalCount > request.Page * request.PageSize;

        return new GetChannelMessagesResult
        {
            Messages = messages,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            HasMore = hasMore
        };
    }
}