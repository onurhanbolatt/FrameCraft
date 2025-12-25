using FrameCraft.Application.Common.Interfaces;

namespace FrameCraft.Infrastructure.Services.MultiTenancy;

/// <summary>
/// Her HTTP isteği için tenant context'i tutar (Scoped)
/// </summary>
public class TenantContext : ITenantContext
{
    private Guid? _currentTenantId;
    private string? _currentTenantSubdomain;
    private bool _isSuperAdmin;
    private Guid? _switchedTenantId;

    public Guid? CurrentTenantId
    {
        get
        {
            // SuperAdmin başka tenant'a geçtiyse onu döndür
            if (_switchedTenantId.HasValue)
                return _switchedTenantId;

            return _currentTenantId;
        }
    }

    public string? CurrentTenantSubdomain => _currentTenantSubdomain;

    public bool IsSuperAdmin => _isSuperAdmin;

    public bool IsTenantFilteringEnabled
    {
        get
        {
            // SuperAdmin ve başka tenant'a geçmediyse filtreleme kapalı
            if (_isSuperAdmin && !_switchedTenantId.HasValue)
                return false;

            return true;
        }
    }

    public void SetTenant(Guid tenantId, string subdomain)
    {
        _currentTenantId = tenantId;
        _currentTenantSubdomain = subdomain;
    }

    public void SetSuperAdminMode(bool isSuperAdmin)
    {
        _isSuperAdmin = isSuperAdmin;
    }

    public void SwitchToTenant(Guid tenantId)
    {
        if (!_isSuperAdmin)
            throw new UnauthorizedAccessException("Sadece SuperAdmin tenant değiştirebilir");

        _switchedTenantId = tenantId;
    }
}
