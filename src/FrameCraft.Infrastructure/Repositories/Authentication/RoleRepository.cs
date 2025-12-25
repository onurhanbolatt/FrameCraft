using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Repositories.Authentication;
using FrameCraft.Infrastructure.Persistence;
using FrameCraft.Infrastructure.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace FrameCraft.Infrastructure.Repositories.Authentication;

public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<List<Role>> GetAllActiveRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }
}