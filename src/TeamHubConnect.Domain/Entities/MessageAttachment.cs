using TeamHubConnect.Domain.Common;

namespace TeamHubConnect.Domain.Entities;

public class MessageAttachment : BaseEntity
{
    public Guid MessageId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string FileUrl { get; private set; } = string.Empty;
    public string? ThumbnailUrl { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public string? Description { get; private set; }

    public virtual Message Message { get; private set; } = null!;

    private MessageAttachment() { }

    public MessageAttachment(
        Guid messageId,
        string fileName,
        string contentType,
        long fileSize,
        string fileUrl,
        string? thumbnailUrl = null,
        int? width = null,
        int? height = null,
        string? description = null)
    {
        MessageId = messageId;
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        FileUrl = fileUrl;
        ThumbnailUrl = thumbnailUrl;
        Width = width;
        Height = height;
        Description = description;
    }

    public bool IsImage => ContentType.StartsWith("image/");
    public bool IsVideo => ContentType.StartsWith("video/");
    public bool IsAudio => ContentType.StartsWith("audio/");
    public bool IsDocument => !IsImage && !IsVideo && !IsAudio;
}