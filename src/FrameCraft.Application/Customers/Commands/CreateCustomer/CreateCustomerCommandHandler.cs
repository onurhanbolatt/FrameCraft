using AutoMapper;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.CRM;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Customers.Commands.CreateCustomer;

/// <summary>
/// CreateCustomerCommand handler
/// ITenantContext ile multi-tenancy desteği
/// </summary>
public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Guid>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateCustomerCommandHandler> _logger;

    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        ITenantContext tenantContext,
        IMapper mapper,
        ILogger<CreateCustomerCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // TenantId'yi context'ten al
        var tenantId = _tenantContext.CurrentTenantId;
        
        if (!tenantId.HasValue || tenantId == Guid.Empty)
            throw new BadRequestException("Tenant bilgisi bulunamadı. Lütfen geçerli bir token ile giriş yapın.");

        // AutoMapper - Command → Entity
        var customer = _mapper.Map<Customer>(request);

        // AutoMapper'ın ignore ettiği alanları manuel set et
        customer.Id = Guid.NewGuid();
        customer.TenantId = tenantId.Value;
        customer.CreatedAt = DateTime.UtcNow;
        customer.IsDeleted = false;

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _customerRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Yeni müşteri oluşturuldu: {CustomerId} - {CustomerName} (Tenant: {TenantId})",
            customer.Id,
            customer.Name,
            tenantId);

        return customer.Id;
    }
}
