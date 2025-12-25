using FrameCraft.Domain.Entities.Common;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Entities.Sales;

namespace FrameCraft.Domain.Entities.Inventory;

public class Frame : TenantEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Thickness { get; set; }
    public decimal? CostPerMeter { get; set; }
    public decimal ProfitMargin { get; set; } = 1.30m;
    public decimal PricePerMeter { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<SaleLine> SaleLines { get; set; } = new List<SaleLine>();
}
