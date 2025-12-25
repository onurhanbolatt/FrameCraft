namespace FrameCraft.Domain.Entities.Common;

/// <summary>
/// Kiracıya özgü entityler için base class
/// </summary>
public abstract class TenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}
