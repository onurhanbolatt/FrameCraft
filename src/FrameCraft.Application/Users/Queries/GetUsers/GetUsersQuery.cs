using FrameCraft.Application.Users.DTOs;
using MediatR;

namespace FrameCraft.Application.Users.Queries.GetUsers;

/// <summary>
/// Kullanıcıları listele
/// SuperAdmin: Tüm kullanıcıları görebilir
/// Admin: Sadece kendi tenant'ındaki kullanıcıları görebilir
/// </summary>
public record GetUsersQuery : IRequest<List<UserSummaryDto>>
{
    /// <summary>
    /// Tenant filtresi (SuperAdmin için)
    /// </summary>
    public Guid? TenantId { get; init; }
    
    /// <summary>
    /// Arama terimi (ad, soyad, e-posta)
    /// </summary>
    public string? SearchTerm { get; init; }
    
    /// <summary>
    /// Aktif/Pasif filtresi
    /// </summary>
    public bool? IsActive { get; init; }
}
