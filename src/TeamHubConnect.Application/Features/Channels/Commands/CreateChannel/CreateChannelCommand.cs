using MediatR;
using TeamHubConnect.Domain.Enums;

namespace TeamHubConnect.Application.Features.Channels.Commands.CreateChannel;

public record CreateChannelCommand : IRequest<CreateChannelResult>
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string? Topic { get; init; }
    public ChannelType Type { get; init; } = ChannelType.Public;
    public Guid WorkspaceId { get; init; }
    public bool IsPrivate { get; init; }
    public List<Guid> MemberIds { get; init; } = [];
    public int MaxMembers { get; init; } = 10000;
    public bool AllowThreads { get; init; } = true;
    public bool AllowReactions { get; init; } = true;
    public bool AllowFileUploads { get; init; } = true;
}

public record CreateChannelResult
{
    public Guid ChannelId { get; init; }
    public string Name { get; init; } = null!;
    public string Type { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}