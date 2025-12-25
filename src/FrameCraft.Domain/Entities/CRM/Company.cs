using FrameCraft.Domain.Entities.Common;
using FrameCraft.Domain.Entities.Inventory;

namespace FrameCraft.Domain.Entities.CRM;

public class Company : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<Frame> Frames { get; set; } = new List<Frame>();
}
