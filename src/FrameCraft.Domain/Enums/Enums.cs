namespace FrameCraft.Domain.Enums;

public enum SaleStatus
{
    Pending = 0,      // Bekliyor
    InProgress = 1,   // İşlemde
    Completed = 2,    // Tamamlandı
    Cancelled = 3     // İptal
}

/// <summary>
/// Kullanıcı rol tipleri
/// Not: Domain.Entities.Authentication.UserRole entity'si ile karıştırılmamalı
/// </summary>
public enum RoleType
{
    SuperAdmin = 0,
    Admin = 1,
    Cashier = 2
}

public enum TenantStatus
{
    Active = 0,
    Inactive = 1,
    Suspended = 2,
    Deleted = 3
}

public enum SaleLineType
{
    Frame = 0,    // Çerçeve
    Product = 1   // Ürün
}
