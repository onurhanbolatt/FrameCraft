using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Repositories.Common;

namespace FrameCraft.Domain.Repositories.Authentication;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAsync(RefreshToken token, CancellationToken cancellationToken = default);
}
