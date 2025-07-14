using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using TeamHubConnect.Application.Auth.Interfaces;

namespace TeamHubConnect.Api.Endpoints.Auth;

[Authorize]
public class LogoutEndpoint : EndpointWithoutRequest
{
    private readonly IAuthService _authService;

    public LogoutEndpoint(IAuthService authService)
    {
        _authService = authService;
    }

    public override void Configure()
    {
        Post("/api/auth/logout");
        Summary(s =>
        {
            s.Summary = "Logout user";
            s.Description = "Revoke all refresh tokens for the current user";
            s.Response(200, "Logout successful");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            await _authService.RevokeTokenAsync(userId, ct);
        }

        await SendOkAsync(new { message = "Logout successful" }, ct);
    }
}