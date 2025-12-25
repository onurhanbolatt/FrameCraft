namespace FrameCraft.Application.Common.Interfaces;

/// <summary>
/// Provides tenant information for file storage operations
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the current tenant ID
    /// </summary>
    Guid? GetCurrentTenantId();
}
