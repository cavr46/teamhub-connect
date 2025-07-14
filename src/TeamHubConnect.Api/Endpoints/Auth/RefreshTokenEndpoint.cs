using FastEndpoints;
using TeamHubConnect.Application.Auth.DTOs;
using TeamHubConnect.Application.Auth.Interfaces;

namespace TeamHubConnect.Api.Endpoints.Auth;

public class RefreshTokenEndpoint : Endpoint<RefreshTokenRequest, AuthResponse>
{
    private readonly IAuthService _authService;

    public RefreshTokenEndpoint(IAuthService authService)
    {
        _authService = authService;
    }

    public override void Configure()
    {
        Post("/api/auth/refresh");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Refresh access token";
            s.Description = "Get a new access token using a valid refresh token";
            s.Response<AuthResponse>(200, "Token refreshed successfully");
            s.Response(401, "Invalid refresh token");
        });
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(req, ct);
            await SendOkAsync(response, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await SendUnauthorizedAsync(ct);
        }
        catch (Exception)
        {
            await SendErrorsAsync(cancellation: ct);
        }
    }
}