using FrameCraft.Domain.Entities.Common;
using FrameCraft.Domain.Entities.Inventory;
using FrameCraft.Domain.Enums;

namespace FrameCraft.Domain.Entities.Sales;

public class SaleLine : TenantEntity
{
    public Guid SaleId { get; set; }
    public SaleLineType LineType { get; set; }
    public Guid? FrameId { get; set; }
    public Guid? ProductId { get; set; }
    
    // Ölçüler (Çerçeve için)
    public int? Height { get; set; }
    public int? Width { get; set; }
    
    // Opsiyonlar
    public bool HasGlass { get; set; }
    public bool HasPassepartout { get; set; }
    public bool HasBox { get; set; }
    
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal LineDiscount { get; set; }
    public decimal LineFinalAmount { get; set; }
    public string? Notes { get; set; }

    // Navigation Properties
    public virtual Sale Sale { get; set; } = null!;
    public virtual Frame? Frame { get; set; }
}
