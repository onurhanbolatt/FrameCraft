using FrameCraft.Domain.Entities.Common;
using FrameCraft.Domain.Entities.Sales;

namespace FrameCraft.Domain.Entities.CRM;

public class Customer : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
