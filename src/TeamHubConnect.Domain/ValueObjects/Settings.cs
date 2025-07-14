namespace TeamHubConnect.Domain.ValueObjects;

public class NotificationSettings
{
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool DesktopNotifications { get; set; } = true;
    public bool SoundNotifications { get; set; } = true;
    public bool MentionNotifications { get; set; } = true;
    public bool DirectMessageNotifications { get; set; } = true;
    public bool ChannelNotifications { get; set; } = true;
    public string DoNotDisturbStart { get; set; } = "22:00";
    public string DoNotDisturbEnd { get; set; } = "08:00";
    public List<string> KeywordNotifications { get; set; } = [];
    public int DigestFrequencyHours { get; set; } = 24;
    public bool WeekendNotifications { get; set; } = false;
}

public class PresenceSettings
{
    public bool ShowOnlineStatus { get; set; } = true;
    public bool ShowLastSeen { get; set; } = true;
    public bool AutoAwayEnabled { get; set; } = true;
    public int AutoAwayMinutes { get; set; } = 10;
    public bool CalendarIntegration { get; set; } = false;
    public string? WorkingHoursStart { get; set; } = "09:00";
    public string? WorkingHoursEnd { get; set; } = "17:00";
    public List<DayOfWeek> WorkingDays { get; set; } = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
}

public class WorkspaceSettings
{
    public bool AllowGuestAccess { get; set; } = false;
    public bool RequireEmailVerification { get; set; } = true;
    public bool AllowPublicChannels { get; set; } = true;
    public bool AllowPrivateChannels { get; set; } = true;
    public bool AllowDirectMessages { get; set; } = true;
    public bool AllowFileUploads { get; set; } = true;
    public bool AllowExternalSharing { get; set; } = false;
    public bool RequireStrongPasswords { get; set; } = true;
    public bool Enable2FA { get; set; } = false;
    public bool AllowBots { get; set; } = true;
    public bool AllowIntegrations { get; set; } = true;
    public bool AllowCustomEmojis { get; set; } = true;
    public bool AllowThreads { get; set; } = true;
    public bool AllowReactions { get; set; } = true;
    public bool AllowMessageEditing { get; set; } = true;
    public bool AllowMessageDeletion { get; set; } = true;
    public int MessageRetentionDays { get; set; } = 365;
    public long MaxFileSize { get; set; } = 50 * 1024 * 1024; // 50MB
    public List<string> AllowedFileTypes { get; set; } = ["jpg", "jpeg", "png", "gif", "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt", "zip"];
}

public class BrandingSettings
{
    public string? PrimaryColor { get; set; } = "#6366f1";
    public string? SecondaryColor { get; set; } = "#8b5cf6";
    public string? AccentColor { get; set; } = "#06b6d4";
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? CustomCss { get; set; }
    public bool ShowBranding { get; set; } = true;
    public string? WelcomeMessage { get; set; }
    public Dictionary<string, string> CustomLabels { get; set; } = [];
}

public class SecuritySettings
{
    public bool RequirePasswordComplexity { get; set; } = true;
    public int MinPasswordLength { get; set; } = 8;
    public bool RequireSpecialCharacters { get; set; } = true;
    public bool RequireNumbers { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public int MaxPasswordAge { get; set; } = 90;
    public int PasswordHistorySize { get; set; } = 5;
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 30;
    public bool RequireMFA { get; set; } = false;
    public bool AllowSocialLogin { get; set; } = true;
    public bool AllowSSOOnly { get; set; } = false;
    public bool EnableAuditLogging { get; set; } = true;
    public bool EnableDLP { get; set; } = false;
    public List<string> RestrictedDomains { get; set; } = [];
    public List<string> AllowedDomains { get; set; } = [];
    public bool EnableE2EEncryption { get; set; } = false;
}

public class ChannelSettings
{
    public bool AllowMemberInvites { get; set; } = true;
    public bool AllowLinkSharing { get; set; } = true;
    public bool RequireApprovalForJoining { get; set; } = false;
    public bool AllowGuestAccess { get; set; } = false;
    public bool EnableMessageModeration { get; set; } = false;
    public bool EnableWordFilter { get; set; } = false;
    public List<string> BannedWords { get; set; } = [];
    public bool EnableSlowMode { get; set; } = false;
    public int SlowModeSeconds { get; set; } = 30;
    public bool EnableAutoArchive { get; set; } = false;
    public int AutoArchiveDays { get; set; } = 30;
    public Dictionary<string, object> CustomSettings { get; set; } = [];
}