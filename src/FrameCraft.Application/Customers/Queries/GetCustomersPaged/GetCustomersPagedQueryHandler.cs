using AutoMapper;
using FrameCraft.Application.Common.Models;
using FrameCraft.Domain.Repositories.CRM;
using MediatR;

namespace FrameCraft.Application.Customers.Queries.GetCustomersPaged;

public class GetCustomersPagedQueryHandler : IRequestHandler<GetCustomersPagedQuery, PagedResult<CustomerListDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public GetCustomersPagedQueryHandler(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<CustomerListDto>> Handle(GetCustomersPagedQuery request, CancellationToken cancellationToken)
    {
        var skip = (request.PageNumber - 1) * request.PageSize;
        var take = request.PageSize;
        var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "name" : request.SortBy!;
        var sortOrder = string.IsNullOrWhiteSpace(request.SortOrder) ? "asc" : request.SortOrder!;

        var (items, totalCount) = await _customerRepository.GetPagedAsync(
            skip,
            take,
            request.Search,
            request.IsActive,
            sortBy,
            sortOrder,
            cancellationToken);

        var dtoItems = _mapper.Map<List<CustomerListDto>>(items);

        return PagedResult<CustomerListDto>.Create(dtoItems, totalCount, request.PageNumber, request.PageSize);
    }
}
