using FrameCraft.Application.Tenants.DTOs;
using MediatR;

namespace FrameCraft.Application.Tenants.Queries.GetTenantById;

/// <summary>
/// Tenant detay bilgisini getir
/// Sadece SuperAdmin kullanabilir
/// </summary>
public record GetTenantByIdQuery : IRequest<TenantDto>
{
    public Guid Id { get; init; }
}
