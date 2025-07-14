namespace TeamHubConnect.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    Guid? CurrentWorkspaceId { get; }
    List<string> Roles { get; }
    bool IsInRole(string role);
    bool IsWorkspaceAdmin(Guid workspaceId);
    bool IsWorkspaceOwner(Guid workspaceId);
}