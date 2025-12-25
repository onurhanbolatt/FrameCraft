using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Users.DTOs;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Authentication;
using FrameCraft.Domain.Repositories.Core;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        ITenantContext tenantContext,
        ILogger<CreateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<CreateUserResultDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Defense in depth: Bu endpoint sadece SuperAdmin tarafından kullanılmalı
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new ForbiddenAccessException("Kullanıcı oluşturma yetkisi bu endpoint üzerinden sadece SuperAdmin'e aittir.");
        }

        // SuperAdmin oluşturma sadece SuperAdmin tarafından yapılabilir
        if (request.IsSuperAdmin && !_tenantContext.IsSuperAdmin)
        {
            throw new ForbiddenAccessException("SuperAdmin kullanıcı oluşturma yetkisi sadece SuperAdmin'e aittir.");
        }

        // Tenant var mı kontrol et
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null || tenant.IsDeleted)
        {
            throw new NotFoundException($"Tenant bulunamadı: {request.TenantId}");
        }

        // Tenant kullanıcı limiti kontrolü
        var existingUserCount = tenant.Users?.Count(u => !u.IsDeleted) ?? 0;
        if (existingUserCount >= tenant.MaxUsers)
        {
            throw new BadRequestException($"Tenant kullanıcı limitine ({tenant.MaxUsers}) ulaşıldı");
        }

        // E-posta benzersizlik kontrolü
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new BadRequestException($"'{request.Email}' e-posta adresi zaten kullanılıyor");
        }

        // Yeni kullanıcı oluştur
        var user = new User
        {
            TenantId = request.TenantId,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            IsSuperAdmin = request.IsSuperAdmin
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Rolleri ata
        var assignedRoles = new List<string>();
        foreach (var roleName in request.Roles)
        {
            var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
            if (role == null)
            {
                // Rol yoksa oluştur
                role = new Role
                {
                    Name = roleName,
                    Description = $"{roleName} rolü"
                };
                await _roleRepository.AddAsync(role, cancellationToken);
                await _roleRepository.SaveChangesAsync(cancellationToken);
            }

            user.UserRoles.Add(new FrameCraft.Domain.Entities.Authentication.UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });
            assignedRoles.Add(roleName);
        }

        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Yeni kullanıcı oluşturuldu: {Email} (Tenant: {TenantId})", user.Email, request.TenantId);

        return new CreateUserResultDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            TenantId = user.TenantId,
            Roles = assignedRoles,
            CreatedAt = user.CreatedAt
        };
    }
}
