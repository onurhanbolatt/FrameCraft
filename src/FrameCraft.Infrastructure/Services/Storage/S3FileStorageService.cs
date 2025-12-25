using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Common.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FrameCraft.Infrastructure.Services.Storage;

public class S3FileStorageService : IFileStorageService, IDisposable
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Settings _settings;
    private readonly ILogger<S3FileStorageService> _logger;
    private readonly ITenantProvider _tenantProvider;
    private bool _disposed;

    // Magic bytes for common file types
    private static readonly Dictionary<string, byte[][]> MagicBytes = new()
    {
        { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
        { ".pdf", new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
        { ".docx", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
        { ".xlsx", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
        { ".doc", new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } },
        { ".xls", new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } },
        { ".webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } },
        { ".bmp", new[] { new byte[] { 0x42, 0x4D } } }
    };

    public S3FileStorageService(
        IOptions<S3Settings> settings,
        ILogger<S3FileStorageService> logger,
        ITenantProvider tenantProvider)
    {
        _settings = settings.Value;
        _logger = logger;
        _tenantProvider = tenantProvider;
        _s3Client = CreateS3Client();
    }

    private IAmazonS3 CreateS3Client()
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(_settings.Region)
        };

        if (_settings.UseLocalStack)
        {
            config.ServiceURL = _settings.LocalStackEndpoint;
            config.ForcePathStyle = true;
            config.UseHttp = true;
        }

        return new AmazonS3Client(
            _settings.AccessKey,
            _settings.SecretKey,
            config);
    }

    public async Task<FileUploadResult> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        long fileSize,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Basic validation
            var validationError = ValidateFile(fileName, fileSize);
            if (validationError != null)
            {
                _logger.LogWarning("File validation failed: {Error}", validationError);
                return FileUploadResult.Failed(validationError);
            }

            // 2. MIME validation (magic bytes check)
            if (_settings.EnableMimeValidation)
            {
                var mimeValidationError = await ValidateMimeTypeAsync(stream, fileName);
                if (mimeValidationError != null)
                {
                    _logger.LogWarning("MIME validation failed for {FileName}: {Error}", fileName, mimeValidationError);
                    return FileUploadResult.Failed(mimeValidationError);
                }

                // Reset stream position after reading
                stream.Position = 0;
            }

            var generatedFileName = GenerateFileName(fileName);
            var fileKey = GenerateFileKey(generatedFileName, folder);

            using var transferUtility = new TransferUtility(_s3Client);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = fileKey,
                BucketName = _settings.BucketName,
                ContentType = contentType,
                CannedACL = S3CannedACL.Private, // Always private!
                AutoCloseStream = false
            };

            // Add metadata
            uploadRequest.Metadata.Add("original-filename", SanitizeMetadataValue(fileName));
            uploadRequest.Metadata.Add("uploaded-at", DateTime.UtcNow.ToString("O"));
            uploadRequest.Metadata.Add("tenant-id", _tenantProvider.GetCurrentTenantId()?.ToString() ?? "system");

            await transferUtility.UploadAsync(uploadRequest, cancellationToken);

            var fileUrl = GetFileUrl(fileKey);

            _logger.LogInformation(
                "File uploaded successfully: {FileName} -> {FileKey} ({Size} bytes)",
                fileName, fileKey, fileSize);

            return FileUploadResult.Succeeded(
                fileKey,
                fileUrl,
                generatedFileName,
                fileName,
                contentType,
                fileSize);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error uploading file: {FileName}", fileName);
            return FileUploadResult.Failed($"Storage error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            return FileUploadResult.Failed("An unexpected error occurred during upload");
        }
    }

    public async Task<FileUploadResult> UploadAsync(
        byte[] fileBytes,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new MemoryStream(fileBytes);
        return await UploadAsync(stream, fileName, contentType, fileBytes.Length, folder, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = fileKey
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);

            _logger.LogInformation("File deleted successfully: {FileKey}", fileKey);
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error deleting file: {FileKey}", fileKey);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileKey}", fileKey);
            return false;
        }
    }

    public async Task<int> DeleteManyAsync(IEnumerable<string> fileKeys, CancellationToken cancellationToken = default)
    {
        try
        {
            var keysList = fileKeys.ToList();
            if (!keysList.Any()) return 0;

            var deleteRequest = new DeleteObjectsRequest
            {
                BucketName = _settings.BucketName,
                Objects = keysList.Select(k => new KeyVersion { Key = k }).ToList()
            };

            var response = await _s3Client.DeleteObjectsAsync(deleteRequest, cancellationToken);

            _logger.LogInformation("Deleted {Count} files successfully", response.DeletedObjects.Count);
            return response.DeletedObjects.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting multiple files");
            return 0;
        }
    }

    public async Task<bool> ExistsAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _settings.BucketName,
                Key = fileKey
            };

            await _s3Client.GetObjectMetadataAsync(request, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence: {FileKey}", fileKey);
            return false;
        }
    }

    public Task<string> GetPreSignedUrlAsync(string fileKey, int expirationMinutes = 0)
    {
        try
        {
            // Use default if not specified, and enforce maximum
            if (expirationMinutes <= 0)
            {
                expirationMinutes = _settings.PreSignedUrlTtlMinutes;
            }

            // Enforce maximum TTL for security
            if (expirationMinutes > _settings.MaxPreSignedUrlTtlMinutes)
            {
                _logger.LogWarning(
                    "Requested TTL {Requested}min exceeds maximum {Max}min, using maximum",
                    expirationMinutes, _settings.MaxPreSignedUrlTtlMinutes);
                expirationMinutes = _settings.MaxPreSignedUrlTtlMinutes;
            }

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _settings.BucketName,
                Key = fileKey,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Verb = HttpVerb.GET
            };

            var url = _s3Client.GetPreSignedURL(request);

            _logger.LogDebug(
                "Generated pre-signed URL for {FileKey}, expires in {Minutes} minutes",
                fileKey, expirationMinutes);

            return Task.FromResult(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pre-signed URL for: {FileKey}", fileKey);
            throw;
        }
    }

    public async Task<FileMetadata?> GetMetadataAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _settings.BucketName,
                Key = fileKey
            };

            var response = await _s3Client.GetObjectMetadataAsync(request, cancellationToken);

            return new FileMetadata
            {
                FileKey = fileKey,
                ContentType = response.Headers.ContentType,
                ContentLength = response.ContentLength,
                LastModified = response.LastModified,
                ETag = response.ETag,
                CustomMetadata = response.Metadata.Keys
                    .ToDictionary(k => k, k => response.Metadata[k])
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for: {FileKey}", fileKey);
            return null;
        }
    }

    public async Task<Stream?> DownloadAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = fileKey
            };

            var response = await _s3Client.GetObjectAsync(request, cancellationToken);

            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("File not found: {FileKey}", fileKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FileKey}", fileKey);
            return null;
        }
    }

    public async Task<FileUploadResult?> CopyAsync(
        string sourceKey,
        string destinationFolder,
        string? newFileName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await GetMetadataAsync(sourceKey, cancellationToken);
            if (metadata == null)
            {
                return FileUploadResult.Failed("Source file not found");
            }

            var fileName = newFileName ?? Path.GetFileName(sourceKey);
            var destinationKey = GenerateFileKey(fileName, destinationFolder);

            var copyRequest = new CopyObjectRequest
            {
                SourceBucket = _settings.BucketName,
                SourceKey = sourceKey,
                DestinationBucket = _settings.BucketName,
                DestinationKey = destinationKey,
                CannedACL = S3CannedACL.Private
            };

            await _s3Client.CopyObjectAsync(copyRequest, cancellationToken);

            var fileUrl = GetFileUrl(destinationKey);

            _logger.LogInformation(
                "File copied successfully: {SourceKey} -> {DestinationKey}",
                sourceKey, destinationKey);

            return FileUploadResult.Succeeded(
                destinationKey,
                fileUrl,
                fileName,
                metadata.CustomMetadata.GetValueOrDefault("original-filename", fileName),
                metadata.ContentType,
                metadata.ContentLength);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying file: {SourceKey}", sourceKey);
            return FileUploadResult.Failed($"Error copying file: {ex.Message}");
        }
    }

    #region Private Methods

    private string? ValidateFile(string fileName, long fileSize)
    {
        if (fileSize <= 0)
            return "File is empty";

        if (fileSize > _settings.MaxFileSizeBytes)
            return $"File size exceeds maximum allowed size of {_settings.MaxFileSizeMB}MB";

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowedExtensions = _settings.GetAllowedExtensions();

        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return $"File type '{extension}' is not allowed. Allowed types: {_settings.AllowedExtensions}";

        return null;
    }

    /// <summary>
    /// Validate file content by checking magic bytes
    /// </summary>
    private async Task<string?> ValidateMimeTypeAsync(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        // If we don't have magic bytes for this type, skip validation
        if (!MagicBytes.TryGetValue(extension, out var expectedBytes))
        {
            return null;
        }

        var maxLength = expectedBytes.Max(b => b.Length);
        var buffer = new byte[maxLength];

        var bytesRead = await stream.ReadAsync(buffer, 0, maxLength);

        if (bytesRead < expectedBytes.Min(b => b.Length))
        {
            return "File is too small to be valid";
        }

        // Check if any of the expected magic bytes match
        var isValid = expectedBytes.Any(expected =>
            buffer.Take(expected.Length).SequenceEqual(expected));

        if (!isValid)
        {
            return $"File content does not match expected format for {extension}";
        }

        return null;
    }

    private string GenerateFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);

        baseName = SanitizeFileName(baseName);

        // Add unique identifier to prevent overwrites and enumeration
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        return $"{baseName}_{timestamp}_{uniqueId}{extension}";
    }

    private string GenerateFileKey(string fileName, string? folder)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenantFolder = tenantId.HasValue ? $"tenant-{tenantId}" : "system";

        var parts = new List<string> { tenantFolder };

        if (!string.IsNullOrWhiteSpace(folder))
        {
            parts.Add(folder.Trim('/'));
        }

        // Add year/month for organization
        parts.Add(DateTime.UtcNow.ToString("yyyy/MM"));
        parts.Add(fileName);

        return string.Join("/", parts);
    }

    private string GetFileUrl(string fileKey)
    {
        if (!string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            return $"{_settings.BaseUrl.TrimEnd('/')}/{fileKey}";
        }

        if (_settings.UseLocalStack)
        {
            return $"{_settings.LocalStackEndpoint}/{_settings.BucketName}/{fileKey}";
        }

        return $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{fileKey}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars()
            .Concat(new[] { ' ', '#', '%', '&', '{', '}', '\\', '<', '>', '*', '?', '$', '!', '\'', '"', ':', '@', '+', '`', '|', '=' })
            .ToArray();

        var sanitized = string.Join("", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        sanitized = sanitized.Replace(" ", "-");

        if (sanitized.Length > 100)
            sanitized = sanitized[..100];

        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }

    private static string SanitizeMetadataValue(string value)
    {
        // S3 metadata values must be ASCII
        return new string(value.Where(c => c < 128).ToArray());
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _s3Client.Dispose();
        _disposed = true;
    }
}