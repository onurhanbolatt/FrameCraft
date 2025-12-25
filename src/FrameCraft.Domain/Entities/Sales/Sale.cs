using FrameCraft.Domain.Entities.Common;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Enums;

namespace FrameCraft.Domain.Entities.Sales;

public class Sale : TenantEntity
{
    public Guid CustomerId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public SaleStatus Status { get; set; } = SaleStatus.Pending;
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation Properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<SaleLine> Lines { get; set; } = new List<SaleLine>();
}
