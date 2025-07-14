using MediatR;

namespace TeamHubConnect.Application.Features.Workspaces.Commands.CreateWorkspace;

public record CreateWorkspaceCommand : IRequest<CreateWorkspaceResult>
{
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public bool CreateGeneralChannel { get; init; } = true;
    public bool CreateRandomChannel { get; init; } = true;
}

public record CreateWorkspaceResult
{
    public Guid WorkspaceId { get; init; }
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public Guid? GeneralChannelId { get; init; }
    public Guid? RandomChannelId { get; init; }
    public DateTime CreatedAt { get; init; }
}