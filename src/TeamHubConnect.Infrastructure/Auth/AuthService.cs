using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamHubConnect.Application.Auth.DTOs;
using TeamHubConnect.Application.Auth.Interfaces;
using TeamHubConnect.Application.Common.Interfaces;
using TeamHubConnect.Domain.Entities;
using TeamHubConnect.Domain.ValueObjects;

namespace TeamHubConnect.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApplicationDbContext context,
        ITokenService tokenService,
        IPasswordService passwordService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordService = passwordService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == request.Email.ToLowerInvariant(), cancellationToken);

        if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is inactive");
        }

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Password != request.ConfirmPassword)
        {
            throw new ArgumentException("Passwords do not match");
        }

        if (!_passwordService.IsValidPassword(request.Password))
        {
            throw new ArgumentException("Password must be at least 8 characters long and contain uppercase, lowercase, digit, and special character");
        }

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == request.Email.ToLowerInvariant(), cancellationToken);

        if (existingUser != null)
        {
            throw new ArgumentException("Email already exists");
        }

        // Create default workspace if needed
        var defaultWorkspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.IsDefault, cancellationToken);

        if (defaultWorkspace == null)
        {
            defaultWorkspace = new Workspace("Default Workspace", "default", true);
            await _context.Workspaces.AddAsync(defaultWorkspace, cancellationToken);
        }

        var user = new User(
            Email.From(request.Email),
            request.DisplayName,
            _passwordService.HashPassword(request.Password),
            defaultWorkspace.Id,
            request.FullName
        );

        await _context.Users.AddAsync(user, cancellationToken);
        
        // Add user to workspace
        var workspaceUser = new WorkspaceUser(defaultWorkspace.Id, user.Id, "member");
        await _context.WorkspaceUsers.AddAsync(workspaceUser, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New user registered: {Email}", request.Email);

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // Revoke old token and create new one
        refreshToken.Revoke();

        var newAuthResponse = await GenerateAuthResponseAsync(refreshToken.User, cancellationToken);
        
        // Update the old token with the new one
        refreshToken.Revoke(replacedByToken: newAuthResponse.RefreshToken);
        
        await _context.SaveChangesAsync(cancellationToken);

        return newAuthResponse;
    }

    public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var principal = _tokenService.ValidateToken(token);
        if (principal == null) return false;

        var userId = _tokenService.GetUserIdFromToken(token);
        if (!userId.HasValue) return false;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        return user != null && user.IsActive;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ArgumentException("Passwords do not match");
        }

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            return false;
        }

        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        if (!_passwordService.IsValidPassword(request.NewPassword))
        {
            throw new ArgumentException("Password must be at least 8 characters long and contain uppercase, lowercase, digit, and special character");
        }

        user.UpdatePassword(_passwordService.HashPassword(request.NewPassword));
        await _context.SaveChangesAsync(cancellationToken);

        // Revoke all refresh tokens
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in refreshTokens)
        {
            token.Revoke("PasswordChanged");
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password changed for user: {UserId}", userId);
        return true;
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == request.Email.ToLowerInvariant(), cancellationToken);

        if (user == null)
        {
            // Don't reveal if email exists
            return true;
        }

        var resetToken = _passwordService.GenerateResetToken();
        var passwordResetToken = new PasswordResetToken(resetToken, user.Id);

        await _context.PasswordResetTokens.AddAsync(passwordResetToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // TODO: Send email with reset token
        _logger.LogInformation("Password reset token generated for user: {Email}", request.Email);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ArgumentException("Passwords do not match");
        }

        if (!_passwordService.IsValidPassword(request.NewPassword))
        {
            throw new ArgumentException("Password must be at least 8 characters long and contain uppercase, lowercase, digit, and special character");
        }

        var resetToken = await _context.PasswordResetTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.Token, cancellationToken);

        if (resetToken == null || !resetToken.IsValid)
        {
            throw new UnauthorizedAccessException("Invalid or expired reset token");
        }

        resetToken.User.UpdatePassword(_passwordService.HashPassword(request.NewPassword));
        resetToken.MarkAsUsed();

        // Revoke all refresh tokens
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == resetToken.UserId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in refreshTokens)
        {
            token.Revoke("PasswordReset");
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset for user: {UserId}", resetToken.UserId);
        return true;
    }

    public async Task<bool> RevokeTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in refreshTokens)
        {
            token.Revoke("UserLogout");
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken(
            refreshToken,
            user.Id,
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        );

        await _context.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(
            user.Id,
            user.Email.Value,
            user.DisplayName,
            user.FullName,
            user.Avatar,
            user.Status.ToString(),
            user.CreatedAt
        );

        return new AuthResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            userDto
        );
    }
}