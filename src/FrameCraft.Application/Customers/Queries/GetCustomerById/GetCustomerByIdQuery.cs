using FrameCraft.Application.Common.Models;
using MediatR;

namespace FrameCraft.Application.Customers.Queries.GetCustomerById;

/// <summary>
/// ID'ye göre müşteri getir
/// </summary>
public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto?>;
