using Fluxor;

namespace TeamHubConnect.Blazor.Store.Workspace;

[FeatureState]
public record WorkspaceState
{
    public bool IsLoading { get; init; } = false;
    public List<WorkspaceDto> Workspaces { get; init; } = [];
    public WorkspaceDto? CurrentWorkspace { get; init; }
    public string? ErrorMessage { get; init; }
}

public record WorkspaceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public string Plan { get; init; } = "Free";
    public bool IsActive { get; init; } = true;
    public int MemberCount { get; init; }
    public List<WorkspaceMemberDto> Members { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}

public record WorkspaceMemberDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? AvatarUrl { get; init; }
    public string Role { get; init; } = "Member";
    public string Status { get; init; } = "Offline";
    public DateTime JoinedAt { get; init; }
    public bool IsActive { get; init; } = true;
}

// Actions
public record LoadWorkspacesAction;
public record LoadWorkspacesSuccessAction(List<WorkspaceDto> Workspaces);
public record LoadWorkspacesFailureAction(string ErrorMessage);
public record SelectWorkspaceAction(Guid WorkspaceId);
public record SelectWorkspaceSuccessAction(WorkspaceDto Workspace);
public record CreateWorkspaceAction(string Name, string Slug, string? Description = null);
public record CreateWorkspaceSuccessAction(WorkspaceDto Workspace);
public record CreateWorkspaceFailureAction(string ErrorMessage);
public record UpdateWorkspaceAction(Guid WorkspaceId, string? Name = null, string? Description = null);
public record JoinWorkspaceAction(string InviteCode);
public record LeaveWorkspaceAction(Guid WorkspaceId);
public record InviteUserAction(Guid WorkspaceId, string Email, string Role = "Member");
public record RemoveUserAction(Guid WorkspaceId, Guid UserId);
public record UpdateUserRoleAction(Guid WorkspaceId, Guid UserId, string Role);