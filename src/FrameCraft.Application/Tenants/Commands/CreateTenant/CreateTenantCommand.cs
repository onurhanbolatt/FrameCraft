using FrameCraft.Application.Tenants.DTOs;
using MediatR;

namespace FrameCraft.Application.Tenants.Commands.CreateTenant;

/// <summary>
/// Yeni tenant (kiracı) oluşturma komutu
/// Sadece SuperAdmin kullanabilir
/// </summary>
public record CreateTenantCommand : IRequest<TenantDto>
{
    /// <summary>
    /// İşletme adı
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Alt domain (benzersiz) - Örn: "cerceveci-mehmet"
    /// </summary>
    public string Subdomain { get; init; } = string.Empty;
    
    /// <summary>
    /// Telefon numarası
    /// </summary>
    public string? Phone { get; init; }
    
    /// <summary>
    /// E-posta adresi
    /// </summary>
    public string? Email { get; init; }
    
    /// <summary>
    /// Abonelik planı - Örn: "Basic", "Premium", "Enterprise"
    /// </summary>
    public string? SubscriptionPlan { get; init; }
    
    /// <summary>
    /// Maksimum kullanıcı sayısı (varsayılan: 5)
    /// </summary>
    public int MaxUsers { get; init; } = 5;
    
    /// <summary>
    /// Depolama kotası MB cinsinden (varsayılan: 1000 MB = 1 GB)
    /// </summary>
    public int StorageQuotaMB { get; init; } = 1000;
    
    /// <summary>
    /// Abonelik bitiş tarihi (null = sınırsız)
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
    
    /// <summary>
    /// İlk admin kullanıcı e-postası (opsiyonel)
    /// Verilirse otomatik admin kullanıcı oluşturulur
    /// </summary>
    public string? AdminEmail { get; init; }
    
    /// <summary>
    /// İlk admin kullanıcı şifresi (AdminEmail verilirse zorunlu)
    /// </summary>
    public string? AdminPassword { get; init; }
    
    /// <summary>
    /// İlk admin kullanıcı adı
    /// </summary>
    public string? AdminFirstName { get; init; }
    
    /// <summary>
    /// İlk admin kullanıcı soyadı
    /// </summary>
    public string? AdminLastName { get; init; }
}
