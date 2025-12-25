using FrameCraft.Domain.Entities.Common;

namespace FrameCraft.Domain.Entities.Storage;

/// <summary>
/// Represents an uploaded file metadata stored in the database
/// </summary>
public class UploadedFile : TenantEntity
{
    public string FileKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string? Description { get; private set; }
    public string? Folder { get; private set; }
    
    /// <summary>
    /// Reference to the entity this file belongs to (e.g., CustomerId, OrderId)
    /// </summary>
    public Guid? EntityId { get; private set; }
    
    /// <summary>
    /// Type of entity this file belongs to (e.g., "Customer", "Order", "Product")
    /// </summary>
    public string? EntityType { get; private set; }
    
    /// <summary>
    /// Category of the file (e.g., "ProfilePhoto", "Invoice", "Contract")
    /// </summary>
    public string? Category { get; private set; }
    
    /// <summary>
    /// User who uploaded the file
    /// </summary>
    public Guid? UploadedBy { get; private set; }
    
    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; private set; }
    
    /// <summary>
    /// Whether the file is publicly accessible
    /// </summary>
    public bool IsPublic { get; private set; }

    private UploadedFile() { } // EF Core

    public static UploadedFile Create(
        string fileKey,
        string fileName,
        string originalFileName,
        string contentType,
        long fileSize,
        Guid? uploadedBy,
        string? folder = null,
        string? description = null,
        Guid? entityId = null,
        string? entityType = null,
        string? category = null)
    {
        return new UploadedFile
        {
            FileKey = fileKey,
            FileName = fileName,
            OriginalFileName = originalFileName,
            ContentType = contentType,
            FileSize = fileSize,
            UploadedBy = uploadedBy,
            Folder = folder,
            Description = description,
            EntityId = entityId,
            EntityType = entityType,
            Category = category,
            IsPublic = false,
            DisplayOrder = 0
        };
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDisplayOrder(int order)
    {
        DisplayOrder = order;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPublic(bool isPublic)
    {
        IsPublic = isPublic;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AttachToEntity(Guid entityId, string entityType, string? category = null)
    {
        EntityId = entityId;
        EntityType = entityType;
        Category = category;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DetachFromEntity()
    {
        EntityId = null;
        EntityType = null;
        Category = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft delete the file
    /// </summary>
    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    public bool IsPdf => ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    public bool IsDocument => ContentType.StartsWith("application/", StringComparison.OrdinalIgnoreCase);
    
    public string FileSizeFormatted
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            if (FileSize < 1024 * 1024 * 1024) return $"{FileSize / (1024.0 * 1024):F1} MB";
            return $"{FileSize / (1024.0 * 1024 * 1024):F2} GB";
        }
    }
}
