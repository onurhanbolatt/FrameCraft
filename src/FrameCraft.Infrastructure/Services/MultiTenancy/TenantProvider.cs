using FrameCraft.Application.Common.Interfaces;

namespace FrameCraft.Infrastructure.Services.MultiTenancy;

/// <summary>
/// Provides tenant information for file storage operations
/// </summary>
public class TenantProvider : ITenantProvider
{
    private readonly ITenantContext _tenantContext;

    public TenantProvider(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public Guid? GetCurrentTenantId()
    {
        var tenantId = _tenantContext.CurrentTenantId;
        return tenantId == Guid.Empty ? null : tenantId;
    }
}
