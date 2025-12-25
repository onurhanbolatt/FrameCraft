using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Repositories.Authentication;
using FrameCraft.Infrastructure.Persistence;
using FrameCraft.Infrastructure.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace FrameCraft.Infrastructure.Repositories.Authentication;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters - Login için tüm tenant'lardaki kullanýcýlarý arayabilmeli
        return await _dbSet
            .IgnoreQueryFilters()
            .Where(u => !u.IsDeleted)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<List<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<User>> GetAllWithRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<User>> GetByTenantIdWithRolesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.TenantId == tenantId)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}