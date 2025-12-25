using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.CRM;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Customers.Commands.DeleteCustomer;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, Unit>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<DeleteCustomerCommandHandler> _logger;

    public DeleteCustomerCommandHandler(
        ICustomerRepository customerRepository,
        ILogger<DeleteCustomerCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);

        if (customer == null)
            throw new NotFoundException("Müşteri", request.Id);  // ✅ Custom exception

        // Soft delete (repository'de implement edildi)
        await _customerRepository.DeleteAsync(customer, cancellationToken);
        await _customerRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Müşteri silindi: {CustomerId} - {CustomerName}",
            customer.Id,
            customer.Name);

        return Unit.Value;
    }
}
