using TeamHubConnect.Domain.Common;

namespace TeamHubConnect.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedBy { get; private set; }
    public string? ReplacedByToken { get; private set; }

    public virtual User User { get; private set; } = null!;

    private RefreshToken() { }

    public RefreshToken(string token, Guid userId, DateTime expiresAt)
    {
        Token = token;
        UserId = userId;
        ExpiresAt = expiresAt;
        IsRevoked = false;
    }

    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;

    public void Revoke(string? revokedBy = null, string? replacedByToken = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        ReplacedByToken = replacedByToken;
    }
}

public class PasswordResetToken : BaseEntity
{
    public string Token { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public virtual User User { get; private set; } = null!;

    private PasswordResetToken() { }

    public PasswordResetToken(string token, Guid userId, int expirationMinutes = 60)
    {
        Token = token;
        UserId = userId;
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        IsUsed = false;
    }

    public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;

    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }
}