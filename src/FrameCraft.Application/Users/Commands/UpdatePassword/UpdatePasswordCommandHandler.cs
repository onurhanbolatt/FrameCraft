using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Users.Commands.UpdatePassword;

public class UpdatePasswordCommandHandler : IRequestHandler<UpdatePasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UpdatePasswordCommandHandler> _logger;

    public UpdatePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService,
        ITenantContext tenantContext,
        ILogger<UpdatePasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
    {
        // Kullanıcıyı bul
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            throw new NotFoundException($"Kullanıcı bulunamadı: {request.UserId}");
        }

        // Yetki kontrolü: Admin reset değilse, kullanıcı sadece kendi şifresini değiştirebilir
        if (!request.IsAdminReset)
        {
            if (_currentUserService.UserId != request.UserId)
            {
                _logger.LogWarning(
                    "Yetkisiz şifre değiştirme girişimi: {CurrentUserId} tried to change password of {TargetUserId}",
                    _currentUserService.UserId, request.UserId);
                throw new ForbiddenAccessException("Sadece kendi şifrenizi değiştirebilirsiniz.");
            }

            if (string.IsNullOrEmpty(request.CurrentPassword))
            {
                throw new BadRequestException("Mevcut şifre gereklidir");
            }

            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                throw new BadRequestException("Mevcut şifre hatalı");
            }
        }
        else
        {
            // Admin reset için SuperAdmin veya aynı tenant'ın Admin'i olmalı
            if (!_tenantContext.IsSuperAdmin)
            {
                // Tenant Admin sadece kendi tenant'ındaki kullanıcıların şifresini sıfırlayabilir
                if (user.TenantId != _tenantContext.CurrentTenantId)
                {
                    _logger.LogWarning(
                        "Cross-tenant şifre sıfırlama girişimi: TenantId {CurrentTenant} tried to reset password of user in TenantId {TargetTenant}",
                        _tenantContext.CurrentTenantId, user.TenantId);
                    throw new ForbiddenAccessException("Bu kullanıcının şifresini sıfırlama yetkiniz yok.");
                }
            }
        }

        // Yeni şifreyi hashle ve kaydet
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Şifre değiştirildi: {UserId} (Admin reset: {IsAdminReset})", 
            user.Id, 
            request.IsAdminReset);

        return true;
    }
}
