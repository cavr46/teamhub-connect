using System.Security.Claims;
using TeamHubConnect.Application.Common.Interfaces;

namespace TeamHubConnect.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? Username => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid? CurrentWorkspaceId
    {
        get
        {
            var workspaceIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("workspace_id")?.Value;
            return Guid.TryParse(workspaceIdClaim, out var workspaceId) ? workspaceId : null;
        }
    }

    public List<string> Roles => _httpContextAccessor.HttpContext?.User?.Claims
        .Where(c => c.Type == ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList() ?? [];

    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }

    public bool IsWorkspaceAdmin(Guid workspaceId)
    {
        var workspaceRoles = _httpContextAccessor.HttpContext?.User?.Claims
            .Where(c => c.Type == "workspace_role" && c.ValueType == workspaceId.ToString())
            .Select(c => c.Value)
            .ToList() ?? [];

        return workspaceRoles.Contains("Admin") || workspaceRoles.Contains("Owner");
    }

    public bool IsWorkspaceOwner(Guid workspaceId)
    {
        var workspaceRoles = _httpContextAccessor.HttpContext?.User?.Claims
            .Where(c => c.Type == "workspace_role" && c.ValueType == workspaceId.ToString())
            .Select(c => c.Value)
            .ToList() ?? [];

        return workspaceRoles.Contains("Owner");
    }
}