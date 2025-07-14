using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using TeamHubConnect.Application.Auth.DTOs;
using TeamHubConnect.Application.Auth.Interfaces;

namespace TeamHubConnect.Api.Endpoints.Auth;

public class GetUserRequest
{
    public Guid UserId { get; set; }
}

[Authorize]
public class GetUserEndpoint : Endpoint<GetUserRequest, UserDto>
{
    private readonly IAuthService _authService;

    public GetUserEndpoint(IAuthService authService)
    {
        _authService = authService;
    }

    public override void Configure()
    {
        Get("/api/users/{userId}");
        Summary(s =>
        {
            s.Summary = "Get user by ID";
            s.Description = "Retrieve user information by user ID";
            s.Response<UserDto>(200, "User found");
            s.Response(404, "User not found");
        });
    }

    public override async Task HandleAsync(GetUserRequest req, CancellationToken ct)
    {
        var user = await _authService.GetUserByIdAsync(req.UserId, ct);
        
        if (user == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var userDto = new UserDto(
            user.Id,
            user.Email.Value,
            user.DisplayName,
            user.FullName,
            user.Avatar,
            user.Status.ToString(),
            user.CreatedAt
        );

        await SendOkAsync(userDto, ct);
    }
}