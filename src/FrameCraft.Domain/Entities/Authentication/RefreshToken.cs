using FrameCraft.Domain.Entities.Common;

namespace FrameCraft.Domain.Entities.Authentication;

/// <summary>
/// Refresh token entity'si
/// Access token'ı yenilemek için kullanılır
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;  

    // Navigation Properties
    public User User { get; set; } = null!;

    // Computed Property
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
