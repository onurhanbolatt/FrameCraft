using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Repositories.Common;

namespace FrameCraft.Domain.Repositories.Authentication;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<User>> GetAllWithRolesAsync(CancellationToken cancellationToken = default);
    Task<List<User>> GetByTenantIdWithRolesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
