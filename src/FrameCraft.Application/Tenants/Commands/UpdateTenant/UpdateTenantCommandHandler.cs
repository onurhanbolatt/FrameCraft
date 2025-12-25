using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Core;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Tenants.Commands.UpdateTenant;

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, bool>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<UpdateTenantCommandHandler> _logger;

    public UpdateTenantCommandHandler(
        ITenantRepository tenantRepository,
        ILogger<UpdateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);

        if (tenant == null || tenant.IsDeleted)
        {
            throw new NotFoundException("Tenant bulunamadı");
        }

        // System Tenant güncellenemez
        if (tenant.IsSystemTenant)
        {
            throw new ForbiddenAccessException("System tenant güncellenemez");
        }

        // Subdomain değiştiyse, başka tenant'ta var mı kontrol et
        if (tenant.Subdomain != request.Subdomain)
        {
            var existingTenant = await _tenantRepository.GetBySubdomainAsync(request.Subdomain, cancellationToken);
            if (existingTenant != null && existingTenant.Id != request.Id)
            {
                throw new BadRequestException("Bu subdomain başka bir tenant tarafından kullanılıyor");
            }
        }

        // Tenant bilgilerini güncelle
        tenant.Name = request.Name;
        tenant.Subdomain = request.Subdomain;
        tenant.Phone = request.Phone;
        tenant.Email = request.Email;
        tenant.Status = request.Status;
        tenant.SubscriptionPlan = request.SubscriptionPlan;
        tenant.MaxUsers = request.MaxUsers;
        tenant.StorageQuotaMB = request.StorageQuotaMB;
        tenant.ExpiresAt = request.ExpiresAt;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _tenantRepository.SaveChangesAsync(cancellationToken); 

        _logger.LogInformation("Tenant güncellendi: {TenantId}", request.Id);

        return true;
    }
}