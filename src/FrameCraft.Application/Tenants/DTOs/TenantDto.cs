using FrameCraft.Domain.Enums;

namespace FrameCraft.Application.Tenants.DTOs;

/// <summary>
/// Tenant DTO - Kiracı bilgileri
/// </summary>
public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public TenantStatus Status { get; set; }
    public string? SubscriptionPlan { get; set; }
    public int MaxUsers { get; set; }
    public int StorageQuotaMB { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
}

/// <summary>
/// Tenant özet DTO - Liste için
/// </summary>
public class TenantSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public TenantStatus Status { get; set; }
    public string? SubscriptionPlan { get; set; }
    public int MaxUsers { get; set; }
    public int UserCount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
