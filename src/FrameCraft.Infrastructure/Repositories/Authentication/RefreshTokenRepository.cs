using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Repositories.Authentication;
using FrameCraft.Infrastructure.Persistence;
using FrameCraft.Infrastructure.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace FrameCraft.Infrastructure.Repositories.Authentication;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task RevokeAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        await UpdateAsync(token, cancellationToken);
    }
}