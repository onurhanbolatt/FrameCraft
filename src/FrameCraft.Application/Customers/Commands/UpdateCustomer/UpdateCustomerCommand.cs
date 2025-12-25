using FrameCraft.Application.Common.Models;
using MediatR;

namespace FrameCraft.Application.Customers.Commands.UpdateCustomer;

public record UpdateCustomerCommand : IRequest<CustomerDto>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
}
