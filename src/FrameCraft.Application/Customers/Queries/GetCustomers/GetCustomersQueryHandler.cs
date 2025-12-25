using FrameCraft.Application.Common.Models;
using FrameCraft.Domain.Repositories.CRM;
using MediatR;

namespace FrameCraft.Application.Customers.Queries.GetCustomers;

/// <summary>
/// GetCustomersQuery handler
/// AutoMapper ile temiz kod!
/// </summary>
public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, List<CustomerListDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public GetCustomersQueryHandler(
        ICustomerRepository customerRepository,
        IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<List<CustomerListDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var customers = await _customerRepository.GetAllAsync(cancellationToken);

        // AutoMapper - Otomatik mapping!
        return _mapper.Map<List<CustomerListDto>>(customers);
    }
}
