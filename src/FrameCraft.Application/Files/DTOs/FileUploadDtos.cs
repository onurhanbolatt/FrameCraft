namespace FrameCraft.Application.Files.DTOs;

// ============================================
// Response DTOs
// ============================================

public record FileUploadResponse(
    bool Success,
    string? FileId,
    string? FileKey,
    string? FileUrl,
    string? FileName,
    string? OriginalFileName,
    string? ContentType,
    long FileSize,
    string? ErrorMessage,
    DateTime? UploadedAt);

public record MultipleFilesUploadResponse(
    int TotalFiles,
    int SuccessCount,
    int FailedCount,
    List<FileUploadResponse> Results);

public record FileInfoResponse(
    string FileId,
    string FileKey,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string FileSizeFormatted,
    string FileUrl,
    string? PreSignedUrl,
    DateTime UploadedAt,
    string? Description,
    Guid? UploadedBy,
    Guid? EntityId,
    string? EntityType,
    string? Category);

public record PreSignedUrlResponse(
    string FileKey,
    string PreSignedUrl,
    DateTime ExpiresAt);

// ============================================
// Validation
// ============================================

public static class FileValidationRules
{
    public static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
    public static readonly string[] DocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" };
    public static readonly string[] AllowedExtensions = ImageExtensions.Concat(DocumentExtensions).ToArray();
    
    public const int MaxFileSizeMB = 10;
    public const long MaxFileSizeBytes = MaxFileSizeMB * 1024 * 1024;
    public const int MaxFileNameLength = 255;
    
    public static bool IsImage(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return ImageExtensions.Contains(extension);
    }
    
    public static bool IsDocument(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return DocumentExtensions.Contains(extension);
    }
    
    public static bool IsAllowed(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }
    
    public static string? Validate(string fileName, long fileSize, string? contentType = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "File name is required";
            
        if (fileName.Length > MaxFileNameLength)
            return $"File name cannot exceed {MaxFileNameLength} characters";
            
        if (fileSize <= 0)
            return "File is empty";
            
        if (fileSize > MaxFileSizeBytes)
            return $"File size cannot exceed {MaxFileSizeMB}MB";
            
        if (!IsAllowed(fileName))
            return $"File type is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}";
            
        return null;
    }
}
