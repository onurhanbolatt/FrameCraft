using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Entities.Common;
using FrameCraft.Domain.Enums;

namespace FrameCraft.Domain.Entities.Core;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public string? SubscriptionPlan { get; set; }
    public int MaxUsers { get; set; } = 5;
    public int StorageQuotaMB { get; set; } = 1000;
    public DateTime? ExpiresAt { get; set; }
    public bool IsSystemTenant { get; set; } = false;

    // Navigation Properties
    public virtual ICollection<TenantFeature> Features { get; set; } = new List<TenantFeature>();
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}

public class TenantFeature : BaseEntity
{
    public Guid TenantId { get; set; }
    public string FeatureName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;

    // Navigation Property
    public virtual Tenant Tenant { get; set; } = null!;
}
