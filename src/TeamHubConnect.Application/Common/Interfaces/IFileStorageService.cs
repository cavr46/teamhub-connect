namespace TeamHubConnect.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<FileUploadResult> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<FileDownloadResult> DownloadFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    Task<string> GenerateDownloadUrlAsync(
        string fileUrl,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}

public class FileUploadResult
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public string Url { get; set; } = "";
    public string? ThumbnailUrl { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

public class FileDownloadResult
{
    public Stream? Stream { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}