using FrameCraft.Application.Authentication.DTOs;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponseDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ITokenService tokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. Refresh token'ı database'den bul
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (refreshToken == null)
            throw new UnauthorizedException("Geçersiz refresh token");

        // 2. Token expire olmuş mu?
        if (refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Expired refresh token kullanılmaya çalışıldı: {TokenId}", refreshToken.Id);
            throw new UnauthorizedException("Refresh token süresi dolmuş");
        }

        // 3. Token revoke edilmiş mi?
        if (refreshToken.IsRevoked)
        {
            _logger.LogWarning("Revoked refresh token kullanılmaya çalışıldı: {TokenId}", refreshToken.Id);
            throw new UnauthorizedException("Refresh token iptal edilmiş");
        }

        // 4. User bilgilerini al
        var user = await _userRepository.GetByIdWithRolesAsync(refreshToken.UserId, cancellationToken);

        if (user == null)
            throw new NotFoundException("Kullanıcı bulunamadı");

        if (!user.IsActive)
            throw new BadRequestException("Kullanıcı hesabı pasif");

        // 5. Rolleri al
        var roles = await _userRepository.GetUserRolesAsync(user.Id, cancellationToken);

        // 6. ESKİ refresh token'ı REVOKE et (Token Rotation!)
        await _refreshTokenRepository.RevokeAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token revoke edildi: {TokenId}", refreshToken.Id);

        // 7. YENİ access token oluştur
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);

        // 8. YENİ refresh token oluştur ve kaydet
        var newRefreshToken = await _tokenService.CreateRefreshTokenAsync(
            user.Id,
            request.IpAddress ?? "unknown",
            cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Token yenilendi - User: {UserId}, IP: {IpAddress}",
            user.Id,
            request.IpAddress);

        // 9. Response oluştur
        return new LoginResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            TenantId = user.TenantId,
            Roles = roles,
            IsSuperAdmin = user.IsSuperAdmin,
            AccessToken = newAccessToken.Token,
            RefreshToken = newRefreshToken.Token,
            AccessTokenExpiration = newAccessToken.ExpiresAt,
            RefreshTokenExpiration = newRefreshToken.ExpiresAt
        };
    }
}
