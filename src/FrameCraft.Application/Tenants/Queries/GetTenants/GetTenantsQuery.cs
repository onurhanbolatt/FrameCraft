using FrameCraft.Application.Tenants.DTOs;
using MediatR;

namespace FrameCraft.Application.Tenants.Queries.GetTenants;

/// <summary>
/// Tüm tenant'ları listele
/// Sadece SuperAdmin kullanabilir
/// </summary>
public record GetTenantsQuery : IRequest<List<TenantSummaryDto>>
{
    /// <summary>
    /// Durum filtresi (opsiyonel)
    /// </summary>
    public Domain.Enums.TenantStatus? Status { get; init; }
    
    /// <summary>
    /// Arama terimi (ad veya subdomain)
    /// </summary>
    public string? SearchTerm { get; init; }
}
