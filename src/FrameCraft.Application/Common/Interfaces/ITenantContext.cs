namespace FrameCraft.Application.Common.Interfaces;

/// <summary>
/// Mevcut HTTP isteğindeki tenant bilgisine erişim sağlar
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Mevcut tenant'ın ID'si (null ise tenant belirlenmemiş veya SuperAdmin tüm tenant'lara erişiyor)
    /// </summary>
    Guid? CurrentTenantId { get; }

    /// <summary>
    /// Mevcut tenant'ın subdomain'i
    /// </summary>
    string? CurrentTenantSubdomain { get; }

    /// <summary>
    /// Kullanıcı SuperAdmin mi?
    /// </summary>
    bool IsSuperAdmin { get; }

    /// <summary>
    /// Tenant context'i ayarla (middleware tarafından kullanılır)
    /// </summary>
    void SetTenant(Guid tenantId, string subdomain);

    /// <summary>
    /// SuperAdmin modunu ayarla
    /// </summary>
    void SetSuperAdminMode(bool isSuperAdmin);

    /// <summary>
    /// SuperAdmin için geçici olarak başka bir tenant'a geç
    /// </summary>
    void SwitchToTenant(Guid tenantId);

    /// <summary>
    /// Tenant filtreleme aktif mi? (SuperAdmin tüm verileri görürken false olabilir)
    /// </summary>
    bool IsTenantFilteringEnabled { get; }
}
