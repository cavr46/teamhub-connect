using TeamHubConnect.Blazor.Store.Auth;

namespace TeamHubConnect.Blazor.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RegisterAsync(string email, string password, string confirmPassword, string displayName, string? fullName = null);
    Task LogoutAsync();
    Task<UserDto?> GetCurrentUserAsync();
    Task<bool> RefreshTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    string? GetToken();
    void SetToken(string token);
    void ClearToken();
}

public record AuthResult(bool IsSuccess, string? Token = null, UserDto? User = null, string? ErrorMessage = null, DateTime? TokenExpiry = null);