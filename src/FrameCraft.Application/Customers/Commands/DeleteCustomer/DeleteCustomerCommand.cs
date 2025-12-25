using MediatR;

namespace FrameCraft.Application.Customers.Commands.DeleteCustomer;

public record DeleteCustomerCommand(Guid Id) : IRequest<Unit>;
