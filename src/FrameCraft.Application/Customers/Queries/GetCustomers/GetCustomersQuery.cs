using FrameCraft.Application.Common.Models;
using MediatR;

namespace FrameCraft.Application.Customers.Queries.GetCustomers;

/// <summary>
/// Tüm müşterileri getir query
/// </summary>
public record GetCustomersQuery : IRequest<List<CustomerListDto>>;
