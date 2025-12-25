using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Core;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Tenants.Commands.DeleteTenant;

public class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, bool>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<DeleteTenantCommandHandler> _logger;

    public DeleteTenantCommandHandler(
        ITenantRepository tenantRepository,
        ILogger<DeleteTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);

        if (tenant == null || tenant.IsDeleted)
        {
            throw new NotFoundException("Tenant bulunamadı");
        }

        // System Tenant silinemez
        if (tenant.IsSystemTenant)
        {
            throw new ForbiddenAccessException("System tenant silinemez");
        }

        // Soft delete
        tenant.IsDeleted = true;
        tenant.DeletedAt = DateTime.UtcNow;
        tenant.Status = Domain.Enums.TenantStatus.Deleted;

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _tenantRepository.SaveChangesAsync(cancellationToken); 

        _logger.LogInformation("Tenant silindi: {TenantId}", request.Id);

        return true;
    }
}