using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Repositories.Common;

namespace FrameCraft.Domain.Repositories.Authentication;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<Role>> GetAllActiveRolesAsync(CancellationToken cancellationToken = default);
}
