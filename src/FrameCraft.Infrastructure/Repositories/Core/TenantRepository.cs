using FrameCraft.Domain.Entities.Core;
using FrameCraft.Domain.Repositories.Core;
using FrameCraft.Infrastructure.Persistence;
using FrameCraft.Infrastructure.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace FrameCraft.Infrastructure.Repositories.Core;

public class TenantRepository : Repository<Tenant>, ITenantRepository
{
    public TenantRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain, cancellationToken);
    }

    public async Task<Tenant?> GetByIdWithUsersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Users)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<Tenant>> GetAllWithUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Users)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}