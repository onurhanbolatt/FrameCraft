using FrameCraft.Domain.Entities.Common;
using FrameCraft.Domain.Entities.Core;

namespace FrameCraft.Domain.Entities.Authentication;

/// <summary>
/// Kullanıcı entity'si
/// Her kullanıcı bir tenant'a aittir (Süper admin hariç)
/// </summary>
public class User : TenantEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsSuperAdmin { get; set; } = false;
    public DateTime? LastLoginAt { get; set; }

    // Navigation Properties
    public Tenant? Tenant { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    // Computed Property
    public string FullName => $"{FirstName} {LastName}";
}
