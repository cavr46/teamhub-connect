using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using TeamHubConnect.Application.Common.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace TeamHubConnect.Infrastructure.Services.Storage;

public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureBlobStorageOptions _options;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<AzureBlobStorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FileUploadResult> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerName = GetContainerName(workspaceId);
            var containerClient = await GetOrCreateContainerAsync(containerName);

            var fileId = Guid.NewGuid();
            var blobName = GenerateBlobName(fileId, fileName);

            var blobClient = containerClient.GetBlobClient(blobName);

            // Upload file with metadata
            var metadata = new Dictionary<string, string>
            {
                { "originalFileName", fileName },
                { "contentType", contentType },
                { "uploadedBy", userId.ToString() },
                { "uploadedAt", DateTime.UtcNow.ToString("O") },
                { "workspaceId", workspaceId.ToString() }
            };

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                Metadata = metadata
            };

            await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);

            // Generate thumbnail if it's an image
            string? thumbnailUrl = null;
            if (IsImageFile(contentType))
            {
                thumbnailUrl = await GenerateThumbnailAsync(fileStream, blobName, containerClient, cancellationToken);
            }

            var fileUrl = blobClient.Uri.ToString();

            _logger.LogInformation("File uploaded successfully: {FileName} -> {BlobName}", fileName, blobName);

            return new FileUploadResult
            {
                FileId = fileId,
                FileName = fileName,
                ContentType = contentType,
                Size = fileStream.Length,
                Url = fileUrl,
                ThumbnailUrl = thumbnailUrl,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            return new FileUploadResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<FileDownloadResult> DownloadFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var blobClient = new BlobClient(uri);

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            var fileName = properties.Value.Metadata.GetValueOrDefault("originalFileName", "download");
            var contentType = properties.Value.ContentType;

            return new FileDownloadResult
            {
                Stream = response.Value.Content,
                FileName = fileName,
                ContentType = contentType,
                Size = response.Value.Details.ContentLength,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from URL: {FileUrl}", fileUrl);
            return new FileDownloadResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var blobClient = new BlobClient(uri);

            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            // Also delete thumbnail if exists
            var thumbnailBlobName = GetThumbnailBlobName(blobClient.Name);
            var thumbnailBlobClient = blobClient.GetParentBlobContainerClient().GetBlobClient(thumbnailBlobName);
            await thumbnailBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("File deleted successfully: {FileUrl}", fileUrl);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return false;
        }
    }

    public async Task<string> GenerateDownloadUrlAsync(
        string fileUrl,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var blobClient = new BlobClient(uri);

            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                return blobClient.GenerateSasUri(sasBuilder).ToString();
            }

            return fileUrl; // Return original URL if SAS cannot be generated
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download URL for file: {FileUrl}", fileUrl);
            return fileUrl;
        }
    }

    public async Task<bool> ValidateFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check file size
            if (fileStream.Length > _options.MaxFileSizeBytes)
            {
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!_options.AllowedFileExtensions.Contains(extension))
            {
                return false;
            }

            // Basic virus scan (placeholder - integrate with actual antivirus service)
            if (await ContainsMaliciousContent(fileStream, cancellationToken))
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file: {FileName}", fileName);
            return false;
        }
    }

    private async Task<BlobContainerClient> GetOrCreateContainerAsync(string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        return containerClient;
    }

    private string GetContainerName(Guid workspaceId)
    {
        return $"workspace-{workspaceId.ToString().ToLowerInvariant()}";
    }

    private string GenerateBlobName(Guid fileId, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        return $"{datePath}/{fileId}{extension}";
    }

    private string GetThumbnailBlobName(string originalBlobName)
    {
        var extension = Path.GetExtension(originalBlobName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalBlobName);
        return Path.GetDirectoryName(originalBlobName) + "/" + nameWithoutExtension + "_thumb" + extension;
    }

    private async Task<string?> GenerateThumbnailAsync(
        Stream originalStream,
        string blobName,
        BlobContainerClient containerClient,
        CancellationToken cancellationToken)
    {
        try
        {
            originalStream.Position = 0;

            using var image = await Image.LoadAsync(originalStream, cancellationToken);
            
            // Resize to thumbnail size
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(300, 300),
                Mode = ResizeMode.Max
            }));

            using var thumbnailStream = new MemoryStream();
            await image.SaveAsJpegAsync(thumbnailStream, cancellationToken);
            thumbnailStream.Position = 0;

            var thumbnailBlobName = GetThumbnailBlobName(blobName);
            var thumbnailBlobClient = containerClient.GetBlobClient(thumbnailBlobName);

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" }
            };

            await thumbnailBlobClient.UploadAsync(thumbnailStream, uploadOptions, cancellationToken);

            return thumbnailBlobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for blob: {BlobName}", blobName);
            return null;
        }
    }

    private bool IsImageFile(string contentType)
    {
        var imageTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/bmp" };
        return imageTypes.Contains(contentType.ToLowerInvariant());
    }

    private async Task<bool> ContainsMaliciousContent(Stream fileStream, CancellationToken cancellationToken)
    {
        // Placeholder for virus scanning logic
        // In production, integrate with Azure Security Center, Windows Defender, or other antivirus services
        await Task.Delay(10, cancellationToken); // Simulate scanning time
        return false;
    }
}

public class AzureBlobStorageOptions
{
    public string ConnectionString { get; set; } = "";
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    public List<string> AllowedFileExtensions { get; set; } = new()
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".csv", ".zip", ".rar", ".7z",
        ".mp4", ".avi", ".mov", ".wmv", ".mp3", ".wav", ".ogg"
    };
}