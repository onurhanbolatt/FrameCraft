using FrameCraft.Domain.Entities.Authentication;

namespace FrameCraft.Application.Common.Interfaces;

/// <summary>
/// JWT token service interface
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Access token oluþtur - JWT token ve expiration döner
    /// </summary>
    /// <returns>Token string ve expiration date tuple</returns>
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user, List<string> roles);

    /// <summary>
    /// Refresh token oluþtur - Random string
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Refresh token oluþtur ve database'e kaydet
    /// </summary>
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, string ipAddress, CancellationToken cancellationToken = default);
}