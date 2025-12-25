namespace FrameCraft.Application.Common.Interfaces;

/// <summary>
/// File storage service interface for S3 operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload a file from stream
    /// </summary>
    Task<FileUploadResult> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        long fileSize,
        string? folder = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Upload a file from byte array
    /// </summary>
    Task<FileUploadResult> UploadAsync(
        byte[] fileBytes,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a file from storage
    /// </summary>
    Task<bool> DeleteAsync(string fileKey, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete multiple files from storage
    /// </summary>
    Task<int> DeleteManyAsync(IEnumerable<string> fileKeys, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a file exists in storage
    /// </summary>
    Task<bool> ExistsAsync(string fileKey, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a pre-signed URL for temporary access
    /// </summary>
    Task<string> GetPreSignedUrlAsync(string fileKey, int expirationMinutes = 60);
    
    /// <summary>
    /// Get file metadata
    /// </summary>
    Task<FileMetadata?> GetMetadataAsync(string fileKey, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Download file as stream
    /// </summary>
    Task<Stream?> DownloadAsync(string fileKey, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Copy a file within storage
    /// </summary>
    Task<FileUploadResult?> CopyAsync(
        string sourceKey, 
        string destinationFolder,
        string? newFileName = null,
        CancellationToken cancellationToken = default);
}

public class FileUploadResult
{
    public bool Success { get; set; }
    public string? FileKey { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public static FileUploadResult Succeeded(
        string fileKey, 
        string fileUrl, 
        string fileName,
        string originalFileName,
        string contentType, 
        long fileSize) => new()
    {
        Success = true,
        FileKey = fileKey,
        FileUrl = fileUrl,
        FileName = fileName,
        OriginalFileName = originalFileName,
        ContentType = contentType,
        FileSize = fileSize
    };
    
    public static FileUploadResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}

public class FileMetadata
{
    public string FileKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public DateTime LastModified { get; set; }
    public string? ETag { get; set; }
    public Dictionary<string, string> CustomMetadata { get; set; } = new();
}
