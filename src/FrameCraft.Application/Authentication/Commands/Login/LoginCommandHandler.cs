using FrameCraft.Application.Authentication.DTOs;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Common.Models;
using FrameCraft.Domain.Enums;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Authentication;
using FrameCraft.Domain.Repositories.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FrameCraft.Application.Authentication.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Kullanıcıyı bul
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            throw new BadRequestException("Email veya şifre hatalı");
        }

        // Şifre kontrolü
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new BadRequestException("Email veya şifre hatalı");
        }

        // Aktif mi kontrol et
        if (!user.IsActive)
        {
            throw new BadRequestException("Kullanıcı hesabı pasif durumda");
        }

        // Tenant status kontrolü (SuperAdmin hariç)
        if (!user.IsSuperAdmin && user.TenantId != Guid.Empty)
        {
            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken);

            if (tenant == null || tenant.IsDeleted)
            {
                throw new BadRequestException("Şirket hesabı bulunamadı");
            }

            if (tenant.Status != TenantStatus.Active)
            {
                var message = tenant.Status switch
                {
                    TenantStatus.Inactive => "Şirket hesabı pasif durumda",
                    TenantStatus.Suspended => "Şirket hesabı askıya alınmış",
                    TenantStatus.Deleted => "Şirket hesabı silinmiş",
                    _ => "Şirket hesabı aktif değil"
                };

                _logger.LogWarning(
                    "Login attempt blocked - Tenant not active: {Email}, TenantId: {TenantId}, Status: {Status}",
                    user.Email, user.TenantId, tenant.Status);

                throw new BadRequestException(message);
            }
        }

        // Rolleri al
        var roles = await _userRepository.GetUserRolesAsync(user.Id, cancellationToken);

        // Access token oluştur (tuple döner!)
        var (accessToken, accessTokenExpiration) = _tokenService.GenerateAccessToken(user, roles);

        // Refresh token oluştur ve kaydet
        var refreshToken = await _tokenService.CreateRefreshTokenAsync(
            user.Id,
            request.IpAddress ?? "unknown",
            cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        // Son login zamanını güncelle
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        return new LoginResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            TenantId = user.TenantId,
            Roles = roles,
            IsSuperAdmin = user.IsSuperAdmin,
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiration = accessTokenExpiration,
            RefreshTokenExpiration = refreshToken.ExpiresAt
        };
    }
}