using FastEndpoints;
using TeamHubConnect.Application.Auth.DTOs;
using TeamHubConnect.Application.Auth.Interfaces;

namespace TeamHubConnect.Api.Endpoints.Auth;

public class LoginEndpoint : Endpoint<LoginRequest, AuthResponse>
{
    private readonly IAuthService _authService;

    public LoginEndpoint(IAuthService authService)
    {
        _authService = authService;
    }

    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "User login";
            s.Description = "Authenticate user with email and password";
            s.ExampleRequest = new LoginRequest("user@example.com", "password123");
            s.Response<AuthResponse>(200, "Login successful");
            s.Response(401, "Invalid credentials");
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        try
        {
            var response = await _authService.LoginAsync(req, ct);
            await SendOkAsync(response, ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            await SendUnauthorizedAsync(ct);
        }
        catch (Exception ex)
        {
            await SendErrorsAsync(cancellation: ct);
        }
    }
}