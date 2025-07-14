using Nest;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using TeamHubConnect.Domain.Entities;
using TeamHubConnect.Application.Common.Interfaces;

namespace TeamHubConnect.Infrastructure.Services.Search;

public class ElasticsearchService : ISearchService
{
    private readonly IElasticClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly ElasticsearchOptions _options;

    public ElasticsearchService(
        IElasticClient client,
        IOptions<ElasticsearchOptions> options,
        ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SearchResult<MessageSearchResult>> SearchMessagesAsync(
        string query,
        Guid workspaceId,
        SearchFilters? filters = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var searchRequest = new SearchRequest<MessageDocument>(_options.MessageIndex)
        {
            Query = BuildMessageQuery(query, workspaceId, filters),
            Size = pageSize,
            From = (page - 1) * pageSize,
            Sort = new List<ISort>
            {
                new FieldSort { Field = "createdAt", Order = SortOrder.Descending }
            },
            Highlight = new Highlight
            {
                Fields = new Dictionary<Field, IHighlightField>
                {
                    { "content", new HighlightField() },
                    { "formattedContent", new HighlightField() }
                }
            }
        };

        var response = await _client.SearchAsync<MessageDocument>(searchRequest, cancellationToken);

        if (!response.IsValid)
        {
            _logger.LogError("Elasticsearch search failed: {Error}", response.DebugInformation);
            return new SearchResult<MessageSearchResult>
            {
                Results = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }

        var results = response.Documents.Select(doc => new MessageSearchResult
        {
            Id = doc.Id,
            Content = doc.Content,
            FormattedContent = doc.FormattedContent,
            AuthorId = doc.AuthorId,
            AuthorName = doc.AuthorName,
            ChannelId = doc.ChannelId,
            ChannelName = doc.ChannelName,
            CreatedAt = doc.CreatedAt,
            Highlights = response.Hits.FirstOrDefault(h => h.Id == doc.Id.ToString())?.Highlight
        }).ToList();

        return new SearchResult<MessageSearchResult>
        {
            Results = results,
            TotalCount = (int)response.Total,
            Page = page,
            PageSize = pageSize,
            HasMore = response.Total > page * pageSize
        };
    }

    public async Task<SearchResult<UserSearchResult>> SearchUsersAsync(
        string query,
        Guid workspaceId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var searchRequest = new SearchRequest<UserDocument>(_options.UserIndex)
        {
            Query = BuildUserQuery(query, workspaceId),
            Size = pageSize,
            From = (page - 1) * pageSize,
            Sort = new List<ISort>
            {
                new FieldSort { Field = "_score", Order = SortOrder.Descending },
                new FieldSort { Field = "displayName.keyword", Order = SortOrder.Ascending }
            }
        };

        var response = await _client.SearchAsync<UserDocument>(searchRequest, cancellationToken);

        if (!response.IsValid)
        {
            _logger.LogError("User search failed: {Error}", response.DebugInformation);
            return new SearchResult<UserSearchResult>();
        }

        var results = response.Documents.Select(doc => new UserSearchResult
        {
            Id = doc.Id,
            Username = doc.Username,
            DisplayName = doc.DisplayName,
            Email = doc.Email,
            AvatarUrl = doc.AvatarUrl,
            Status = doc.Status
        }).ToList();

        return new SearchResult<UserSearchResult>
        {
            Results = results,
            TotalCount = (int)response.Total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task IndexMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        var document = new MessageDocument
        {
            Id = message.Id,
            Content = message.Content,
            FormattedContent = message.FormattedContent,
            AuthorId = message.AuthorId,
            AuthorName = message.Author?.DisplayName ?? "",
            ChannelId = message.ChannelId,
            ChannelName = message.Channel?.Name ?? "",
            WorkspaceId = message.Channel?.WorkspaceId ?? Guid.Empty,
            CreatedAt = message.CreatedAt,
            Type = message.Type.ToString(),
            HasAttachments = message.Attachments.Any(),
            AttachmentTypes = message.Attachments.Select(a => a.ContentType).ToList()
        };

        var response = await _client.IndexDocumentAsync(document, cancellationToken);
        
        if (!response.IsValid)
        {
            _logger.LogError("Failed to index message {MessageId}: {Error}", message.Id, response.DebugInformation);
        }
    }

    public async Task IndexUserAsync(User user, Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var document = new UserDocument
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email.Value,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status.ToString(),
            WorkspaceId = workspaceId,
            Skills = user.Skills.Select(s => s.Name).ToList()
        };

        var response = await _client.IndexDocumentAsync(document, cancellationToken);
        
        if (!response.IsValid)
        {
            _logger.LogError("Failed to index user {UserId}: {Error}", user.Id, response.DebugInformation);
        }
    }

    public async Task DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var response = await _client.DeleteAsync<MessageDocument>(messageId, idx => idx.Index(_options.MessageIndex), cancellationToken);
        
        if (!response.IsValid)
        {
            _logger.LogError("Failed to delete message {MessageId}: {Error}", messageId, response.DebugInformation);
        }
    }

    private QueryContainer BuildMessageQuery(string query, Guid workspaceId, SearchFilters? filters)
    {
        var queries = new List<QueryContainer>
        {
            new TermQuery { Field = "workspaceId", Value = workspaceId }
        };

        if (!string.IsNullOrEmpty(query))
        {
            queries.Add(new MultiMatchQuery
            {
                Query = query,
                Fields = new[] { "content^2", "formattedContent", "authorName" },
                Type = TextQueryType.BestFields,
                Fuzziness = Fuzziness.Auto
            });
        }

        if (filters != null)
        {
            if (filters.ChannelIds?.Any() == true)
            {
                queries.Add(new TermsQuery { Field = "channelId", Terms = filters.ChannelIds });
            }

            if (filters.AuthorIds?.Any() == true)
            {
                queries.Add(new TermsQuery { Field = "authorId", Terms = filters.AuthorIds });
            }

            if (filters.DateFrom.HasValue)
            {
                queries.Add(new DateRangeQuery { Field = "createdAt", GreaterThanOrEqualTo = filters.DateFrom.Value });
            }

            if (filters.DateTo.HasValue)
            {
                queries.Add(new DateRangeQuery { Field = "createdAt", LessThanOrEqualTo = filters.DateTo.Value });
            }

            if (filters.HasAttachments.HasValue)
            {
                queries.Add(new TermQuery { Field = "hasAttachments", Value = filters.HasAttachments.Value });
            }
        }

        return new BoolQuery { Must = queries };
    }

    private QueryContainer BuildUserQuery(string query, Guid workspaceId)
    {
        var queries = new List<QueryContainer>
        {
            new TermQuery { Field = "workspaceId", Value = workspaceId }
        };

        if (!string.IsNullOrEmpty(query))
        {
            queries.Add(new MultiMatchQuery
            {
                Query = query,
                Fields = new[] { "displayName^3", "username^2", "email", "skills" },
                Type = TextQueryType.BestFields,
                Fuzziness = Fuzziness.Auto
            });
        }

        return new BoolQuery { Must = queries };
    }
}

public class ElasticsearchOptions
{
    public string ConnectionString { get; set; } = "http://localhost:9200";
    public string MessageIndex { get; set; } = "messages";
    public string UserIndex { get; set; } = "users";
    public string ChannelIndex { get; set; } = "channels";
}

public class MessageDocument
{
    public Guid Id { get; set; }
    public string Content { get; set; } = "";
    public string? FormattedContent { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public Guid ChannelId { get; set; }
    public string ChannelName { get; set; } = "";
    public Guid WorkspaceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Type { get; set; } = "";
    public bool HasAttachments { get; set; }
    public List<string> AttachmentTypes { get; set; } = [];
}

public class UserDocument
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = "";
    public Guid WorkspaceId { get; set; }
    public List<string> Skills { get; set; } = [];
}