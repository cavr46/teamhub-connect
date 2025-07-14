using TeamHubConnect.Domain.Common;
using TeamHubConnect.Domain.Enums;
using TeamHubConnect.Domain.ValueObjects;

namespace TeamHubConnect.Domain.Entities;

public class User : BaseAuditableEntity
{
    public string Username { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? TimeZone { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Offline;
    public string? StatusMessage { get; private set; }
    public DateTime? StatusExpiresAt { get; private set; }
    public DateTime? LastSeenAt { get; private set; }
    public UserRole Role { get; private set; } = UserRole.Member;
    public bool IsEmailVerified { get; private set; }
    public bool IsPhoneVerified { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }
    public string PasswordHash { get; private set; } = null!;
    public Guid WorkspaceId { get; private set; }
    public string? FullName { get; private set; }
    public string? Avatar { get; private set; }
    public NotificationSettings NotificationSettings { get; private set; } = new();
    public PresenceSettings PresenceSettings { get; private set; } = new();
    
    private readonly List<WorkspaceUser> _workspaces = [];
    public IReadOnlyCollection<WorkspaceUser> Workspaces => _workspaces.AsReadOnly();

    private readonly List<UserSkill> _skills = [];
    public IReadOnlyCollection<UserSkill> Skills => _skills.AsReadOnly();

    private User() { }

    public User(Email email, string displayName, string passwordHash, Guid workspaceId, string? fullName = null)
    {
        Email = email;
        Username = email.Value.Split('@')[0];
        DisplayName = displayName;
        PasswordHash = passwordHash;
        WorkspaceId = workspaceId;
        FullName = fullName;
        IsActive = true;
        Role = UserRole.Member;
    }

    public static User Create(
        string username,
        Email email,
        string displayName,
        string? firstName = null,
        string? lastName = null)
    {
        var user = new User
        {
            Username = username,
            Email = email,
            DisplayName = displayName,
            FirstName = firstName,
            LastName = lastName
        };

        return user;
    }

    public void UpdateProfile(
        string? displayName = null,
        string? firstName = null,
        string? lastName = null,
        string? avatarUrl = null,
        string? phoneNumber = null,
        string? timeZone = null)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            DisplayName = displayName;
            
        FirstName = firstName;
        LastName = lastName;
        AvatarUrl = avatarUrl;
        PhoneNumber = phoneNumber;
        TimeZone = timeZone;
        
        MarkAsUpdated();
    }

    public void UpdateStatus(UserStatus status, string? message = null, DateTime? expiresAt = null)
    {
        Status = status;
        StatusMessage = message;
        StatusExpiresAt = expiresAt;
        MarkAsUpdated();
    }

    public void UpdateLastSeen()
    {
        LastSeenAt = DateTime.UtcNow;
        if (Status == UserStatus.Offline)
        {
            Status = UserStatus.Online;
        }
        MarkAsUpdated();
    }

    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        MarkAsUpdated();
    }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        MarkAsUpdated();
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdateLastSeen();
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
        MarkAsUpdated();
    }

    public void VerifyPhone()
    {
        IsPhoneVerified = true;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        Status = UserStatus.Offline;
        MarkAsUpdated();
    }

    public void UpdateNotificationSettings(NotificationSettings settings)
    {
        NotificationSettings = settings;
        MarkAsUpdated();
    }

    public void UpdatePresenceSettings(PresenceSettings settings)
    {
        PresenceSettings = settings;
        MarkAsUpdated();
    }

    public void AddSkill(string name, SkillLevel level)
    {
        if (_skills.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return;

        _skills.Add(new UserSkill { Name = name, Level = level });
        MarkAsUpdated();
    }

    public void RemoveSkill(string name)
    {
        var skill = _skills.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (skill != null)
        {
            _skills.Remove(skill);
            MarkAsUpdated();
        }
    }
}

public class UserSkill
{
    public string Name { get; set; } = null!;
    public SkillLevel Level { get; set; }
}

public enum SkillLevel
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3,
    Expert = 4
}