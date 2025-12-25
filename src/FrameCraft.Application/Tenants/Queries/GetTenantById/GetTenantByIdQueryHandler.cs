using FrameCraft.Application.Tenants.DTOs;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Core;
using MediatR;

namespace FrameCraft.Application.Tenants.Queries.GetTenantById;

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantDto>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantByIdQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantDto> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdWithUsersAsync(request.Id, cancellationToken);

        if (tenant == null)
        {
            throw new NotFoundException($"Tenant bulunamadı: {request.Id}");
        }

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            Phone = tenant.Phone,
            Email = tenant.Email,
            Status = tenant.Status,
            SubscriptionPlan = tenant.SubscriptionPlan,
            MaxUsers = tenant.MaxUsers,
            StorageQuotaMB = tenant.StorageQuotaMB,
            ExpiresAt = tenant.ExpiresAt,
            CreatedAt = tenant.CreatedAt,
            UserCount = tenant.Users?.Count ?? 0
        };
    }
}
