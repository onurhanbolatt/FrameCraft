using FrameCraft.Application.Common.Models;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.CRM;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Customers.Commands.UpdateCustomer;

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateCustomerCommandHandler> _logger;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IMapper mapper,
        ILogger<UpdateCustomerCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);

        if (customer == null)
            throw new NotFoundException("Müşteri", request.Id);  // ✅ Custom exception

        // Manuel update (Command → Entity mapping)
        customer.Name = request.Name;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.Address = request.Address;
        customer.Notes = request.Notes;
        customer.IsActive = request.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _customerRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Müşteri güncellendi: {CustomerId} - {CustomerName}",
            customer.Id,
            customer.Name);

        // AutoMapper - Entity → DTO
        return _mapper.Map<CustomerDto>(customer);
    }
}
