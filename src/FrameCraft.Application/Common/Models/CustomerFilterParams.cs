namespace FrameCraft.Application.Common.Models;

/// <summary>
/// Müşteri filtreleme ve sıralama parametreleri
/// Query string'den gelir: ?search=ahmet&sortBy=name&sortOrder=asc&isActive=true
/// </summary>
public class CustomerFilterParams : PaginationParams
{
    /// <summary>
    /// Sadece aktif müşteriler (null = hepsi)
    /// </summary>
    public bool? IsActive { get; set; }
    
    // Search, SortBy, SortOrder base class PaginationParams'tan geliyor
}
