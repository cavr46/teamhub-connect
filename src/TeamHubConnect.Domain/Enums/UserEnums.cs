namespace TeamHubConnect.Domain.Enums;

public enum UserStatus
{
    Online = 1,
    Away = 2,
    Busy = 3,
    DoNotDisturb = 4,
    Offline = 5,
    Invisible = 6
}

public enum UserRole
{
    Guest = 1,
    Member = 2,
    Moderator = 3,
    Admin = 4,
    Owner = 5
}

public enum WorkspaceRole
{
    Guest = 1,
    Member = 2,
    Admin = 3,
    Owner = 4
}

public enum ChannelRole
{
    Member = 1,
    Moderator = 2,
    Admin = 3
}

public enum SubscriptionPlan
{
    Free = 1,
    Pro = 2,
    Business = 3,
    Enterprise = 4
}