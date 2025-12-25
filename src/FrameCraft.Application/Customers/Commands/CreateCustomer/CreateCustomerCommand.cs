using FrameCraft.Application.Common.Models;
using MediatR;

namespace FrameCraft.Application.Customers.Commands.CreateCustomer;

/// <summary>
/// Yeni müşteri oluştur command
/// </summary>
public record CreateCustomerCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; } = true;
}
