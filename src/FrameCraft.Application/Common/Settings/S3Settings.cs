namespace FrameCraft.Application.Common.Settings;

public class S3Settings
{
    public const string SectionName = "AWS:S3";

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "eu-central-1";
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Maximum file size in MB
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 10;

    /// <summary>
    /// Allowed file extensions (comma separated)
    /// </summary>
    public string AllowedExtensions { get; set; } = ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx";

    /// <summary>
    /// Use LocalStack for development
    /// </summary>
    public bool UseLocalStack { get; set; } = false;

    /// <summary>
    /// LocalStack endpoint URL
    /// </summary>
    public string LocalStackEndpoint { get; set; } = "http://localhost:4566";

    /// <summary>
    /// Default PreSignedUrl TTL in minutes (güvenlik için kýsa tutulmalý)
    /// </summary>
    public int PreSignedUrlTtlMinutes { get; set; } = 15;

    /// <summary>
    /// Maximum PreSignedUrl TTL in minutes
    /// </summary>
    public int MaxPreSignedUrlTtlMinutes { get; set; } = 60;

    /// <summary>
    /// Enable MIME type validation (magic bytes check)
    /// </summary>
    public bool EnableMimeValidation { get; set; } = true;

    public long MaxFileSizeBytes => MaxFileSizeMB * 1024 * 1024;

    public string[] GetAllowedExtensions() =>
        AllowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}