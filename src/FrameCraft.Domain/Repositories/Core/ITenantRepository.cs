using FrameCraft.Domain.Entities.Core;
using FrameCraft.Domain.Repositories.Common;

namespace FrameCraft.Domain.Repositories.Core;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByIdWithUsersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Tenant>> GetAllWithUsersAsync(CancellationToken cancellationToken = default);
}
