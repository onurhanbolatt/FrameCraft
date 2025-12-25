using FrameCraft.Application.Tenants.DTOs;
using FrameCraft.Domain.Repositories.Core;
using MediatR;

namespace FrameCraft.Application.Tenants.Queries.GetTenants;

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, List<TenantSummaryDto>>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantsQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<List<TenantSummaryDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await _tenantRepository.GetAllWithUsersAsync(cancellationToken);

        var query = tenants.AsQueryable();

        // System Tenant'ý listeden gizle
        query = query.Where(t => !t.IsSystemTenant);

        // Durum filtresi
        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        // Arama filtresi
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(searchLower) ||
                t.Subdomain.ToLower().Contains(searchLower));
        }

        var result = new List<TenantSummaryDto>();

        foreach (var t in query)
        {
            result.Add(new TenantSummaryDto
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                Phone = t.Phone,
                Status = t.Status,
                SubscriptionPlan = t.SubscriptionPlan,
                MaxUsers = t.MaxUsers,
                UserCount = t.Users != null ? t.Users.Count : 0,
                ExpiresAt = t.ExpiresAt,
                CreatedAt = t.CreatedAt
            });
        }

        return result;
    }
}