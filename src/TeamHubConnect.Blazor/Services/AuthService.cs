using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using TeamHubConnect.Blazor.Store.Auth;

namespace TeamHubConnect.Blazor.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private const string TokenKey = "authToken";
    private const string UserKey = "currentUser";

    public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        try
        {
            var request = new { Email = email, Password = password };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result != null)
                {
                    await _localStorage.SetItemAsync(TokenKey, result.AccessToken);
                    await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);
                    await _localStorage.SetItemAsync(UserKey, result.User);
                    
                    SetAuthorizationHeader(result.AccessToken);
                    
                    return new AuthResult(true, result.AccessToken, result.User, TokenExpiry: result.ExpiresAt);
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return new AuthResult(false, ErrorMessage: errorContent);
        }
        catch (Exception ex)
        {
            return new AuthResult(false, ErrorMessage: ex.Message);
        }
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string confirmPassword, string displayName, string? fullName = null)
    {
        try
        {
            var request = new { Email = email, Password = password, ConfirmPassword = confirmPassword, DisplayName = displayName, FullName = fullName };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result != null)
                {
                    await _localStorage.SetItemAsync(TokenKey, result.AccessToken);
                    await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);
                    await _localStorage.SetItemAsync(UserKey, result.User);
                    
                    SetAuthorizationHeader(result.AccessToken);
                    
                    return new AuthResult(true, result.AccessToken, result.User, TokenExpiry: result.ExpiresAt);
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return new AuthResult(false, ErrorMessage: errorContent);
        }
        catch (Exception ex)
        {
            return new AuthResult(false, ErrorMessage: ex.Message);
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        await _localStorage.RemoveItemAsync(UserKey);
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            return await _localStorage.GetItemAsync<UserDto>(UserKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            var request = new { RefreshToken = refreshToken };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result != null)
                {
                    await _localStorage.SetItemAsync(TokenKey, result.AccessToken);
                    await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);
                    await _localStorage.SetItemAsync(UserKey, result.User);
                    SetAuthorizationHeader(result.AccessToken);
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _localStorage.GetItemAsync<string>(TokenKey);
        return !string.IsNullOrEmpty(token);
    }

    public string? GetToken()
    {
        return _httpClient.DefaultRequestHeaders.Authorization?.Parameter;
    }

    public void SetToken(string token)
    {
        SetAuthorizationHeader(token);
    }

    public void ClearToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    private void SetAuthorizationHeader(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);
}