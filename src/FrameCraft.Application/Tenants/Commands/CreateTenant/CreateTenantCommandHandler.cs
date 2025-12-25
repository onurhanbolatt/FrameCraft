using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Tenants.DTOs;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Entities.Core;
using FrameCraft.Domain.Enums;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Authentication;
using FrameCraft.Domain.Repositories.Core;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Tenants.Commands.CreateTenant;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        ITenantContext tenantContext,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<TenantDto> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        // Defense in depth: Handler seviyesinde SuperAdmin kontrolü
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new ForbiddenAccessException("Tenant oluşturma yetkisi sadece SuperAdmin'e aittir.");
        }

        // Subdomain benzersizlik kontrolü
        var existingTenant = await _tenantRepository.GetBySubdomainAsync(request.Subdomain, cancellationToken);
        if (existingTenant != null)
        {
            throw new BadRequestException($"'{request.Subdomain}' subdomain'i zaten kullanılıyor");
        }

        // Admin e-posta benzersizlik kontrolü (eğer admin oluşturulacaksa)
        if (!string.IsNullOrEmpty(request.AdminEmail))
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.AdminEmail, cancellationToken);
            if (existingUser != null)
            {
                throw new BadRequestException($"'{request.AdminEmail}' e-posta adresi zaten kullanılıyor");
            }
        }

        // Yeni tenant oluştur
        var tenant = new Tenant
        {
            Name = request.Name,
            Subdomain = request.Subdomain.ToLowerInvariant(),
            Phone = request.Phone,
            Email = request.Email,
            Status = TenantStatus.Active,
            SubscriptionPlan = request.SubscriptionPlan ?? "Basic",
            MaxUsers = request.MaxUsers,
            StorageQuotaMB = request.StorageQuotaMB,
            ExpiresAt = request.ExpiresAt
        };

        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _tenantRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Yeni tenant oluşturuldu: {TenantName} ({Subdomain})", tenant.Name, tenant.Subdomain);

        // Admin kullanıcı oluştur (eğer bilgiler verilmişse)
        int userCount = 0;
        if (!string.IsNullOrEmpty(request.AdminEmail) && !string.IsNullOrEmpty(request.AdminPassword))
        {
            // Admin rolünü bul veya oluştur
            var adminRole = await _roleRepository.GetByNameAsync("Admin", cancellationToken);
            if (adminRole == null)
            {
                adminRole = new Role
                {
                    Name = "Admin",
                    Description = "Tenant yöneticisi"
                };
                await _roleRepository.AddAsync(adminRole, cancellationToken);
                await _roleRepository.SaveChangesAsync(cancellationToken);
            }

            // Admin kullanıcı oluştur
            var adminUser = new User
            {
                TenantId = tenant.Id,
                Email = request.AdminEmail,
                PasswordHash = _passwordHasher.HashPassword(request.AdminPassword),
                FirstName = request.AdminFirstName ?? "Admin",
                LastName = request.AdminLastName ?? "User",
                IsActive = true,
                IsSuperAdmin = false
            };

            await _userRepository.AddAsync(adminUser, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            // Kullanıcıya Admin rolü ata
            adminUser.UserRoles.Add(new FrameCraft.Domain.Entities.Authentication.UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });

            await _userRepository.SaveChangesAsync(cancellationToken);

            userCount = 1;
            _logger.LogInformation("Tenant için admin kullanıcı oluşturuldu: {Email}", request.AdminEmail);
        }

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            Phone = tenant.Phone,
            Email = tenant.Email,
            Status = tenant.Status,
            SubscriptionPlan = tenant.SubscriptionPlan,
            MaxUsers = tenant.MaxUsers,
            StorageQuotaMB = tenant.StorageQuotaMB,
            ExpiresAt = tenant.ExpiresAt,
            CreatedAt = tenant.CreatedAt,
            UserCount = userCount
        };
    }
}
