using FrameCraft.Application.Common.Models;
using MediatR;

namespace FrameCraft.Application.Customers.Queries.GetCustomersPaged;

/// <summary>
/// Sayfalanmış müşteri listesi query
/// </summary>
public record GetCustomersPagedQuery : IRequest<PagedResult<CustomerListDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public string? SortBy { get; init; } = "name";
    public string? SortOrder { get; init; } = "asc";
}
