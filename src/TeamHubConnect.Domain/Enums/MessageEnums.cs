namespace TeamHubConnect.Domain.Enums;

public enum MessageType
{
    Text = 1,
    Image = 2,
    Video = 3,
    Audio = 4,
    File = 5,
    Code = 6,
    Poll = 7,
    Announcement = 8,
    System = 9,
    Bot = 10,
    Call = 11,
    ScreenShare = 12
}

public enum MessagePriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

public enum MessageDeliveryStatus
{
    Draft = 1,
    Scheduled = 2,
    Sending = 3,
    Sent = 4,
    Delivered = 5,
    Read = 6,
    Failed = 7
}

public enum MentionType
{
    User = 1,
    Channel = 2,
    Here = 3,
    Everyone = 4,
    Role = 5
}

public enum ChannelType
{
    Public = 1,
    Private = 2,
    DirectMessage = 3,
    GroupMessage = 4,
    Announcement = 5,
    Voice = 6,
    Video = 7
}