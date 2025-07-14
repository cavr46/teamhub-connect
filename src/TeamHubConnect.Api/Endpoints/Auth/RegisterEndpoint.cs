using FastEndpoints;
using FluentValidation;
using TeamHubConnect.Application.Auth.DTOs;
using TeamHubConnect.Application.Auth.Interfaces;

namespace TeamHubConnect.Api.Endpoints.Auth;

public class RegisterEndpoint : Endpoint<RegisterRequest, AuthResponse>
{
    private readonly IAuthService _authService;

    public RegisterEndpoint(IAuthService authService)
    {
        _authService = authService;
    }

    public override void Configure()
    {
        Post("/api/auth/register");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Register new user";
            s.Description = "Create a new user account";
            s.ExampleRequest = new RegisterRequest("user@example.com", "Password123!", "Password123!", "John Doe", "John Doe");
            s.Response<AuthResponse>(201, "Registration successful");
            s.Response(400, "Validation error");
            s.Response(409, "Email already exists");
        });
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        try
        {
            var response = await _authService.RegisterAsync(req, ct);
            await SendCreatedAtAsync<GetUserEndpoint>(
                new { userId = response.User.Id },
                response,
                cancellation: ct);
        }
        catch (ArgumentException ex)
        {
            AddError(ex.Message);
            await SendErrorsAsync(cancellation: ct);
        }
        catch (Exception ex)
        {
            await SendErrorsAsync(cancellation: ct);
        }
    }
}

public class RegisterRequestValidator : Validator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"\d").WithMessage("Password must contain at least one digit")
            .Matches(@"[!@#$%^&*(),.?"":{}|<>]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MinimumLength(2).WithMessage("Display name must be at least 2 characters")
            .MaximumLength(50).WithMessage("Display name cannot exceed 50 characters");
    }
}