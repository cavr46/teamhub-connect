using Fluxor;

namespace TeamHubConnect.Blazor.Store.Auth;

[FeatureState]
public record AuthState
{
    public bool IsAuthenticated { get; init; } = false;
    public bool IsLoading { get; init; } = false;
    public string? Token { get; init; }
    public UserDto? CurrentUser { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? TokenExpiry { get; init; }
}

public record UserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? AvatarUrl { get; init; }
    public string Status { get; init; } = "Offline";
    public string? StatusMessage { get; init; }
    public List<string> Roles { get; init; } = [];
}

// Actions
public record LoginAction(string Email, string Password);
public record LoginSuccessAction(string Token, UserDto User, DateTime TokenExpiry);
public record LoginFailureAction(string ErrorMessage);
public record LogoutAction;
public record LoadUserAction;
public record LoadUserSuccessAction(UserDto User);
public record LoadUserFailureAction(string ErrorMessage);
public record UpdateUserStatusAction(string Status, string? Message = null);
public record ClearErrorAction;