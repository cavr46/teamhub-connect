using TeamHubConnect.Domain.Entities;

namespace TeamHubConnect.Application.Common.Interfaces;

public interface ISearchService
{
    Task<SearchResult<MessageSearchResult>> SearchMessagesAsync(
        string query,
        Guid workspaceId,
        SearchFilters? filters = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<SearchResult<UserSearchResult>> SearchUsersAsync(
        string query,
        Guid workspaceId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task IndexMessageAsync(Message message, CancellationToken cancellationToken = default);
    Task IndexUserAsync(User user, Guid workspaceId, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
}

public class SearchResult<T>
{
    public List<T> Results { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}

public class SearchFilters
{
    public List<Guid>? ChannelIds { get; set; }
    public List<Guid>? AuthorIds { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool? HasAttachments { get; set; }
    public List<string>? FileTypes { get; set; }
}

public class MessageSearchResult
{
    public Guid Id { get; set; }
    public string Content { get; set; } = "";
    public string? FormattedContent { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public Guid ChannelId { get; set; }
    public string ChannelName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public object? Highlights { get; set; }
}

public class UserSearchResult
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = "";
}

public class ChannelSearchResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "";
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}