using AutoMapper;
using FluentAssertions;
using FrameCraft.Application.Common.Mappings;
using FrameCraft.Application.Customers.Queries.GetCustomersPaged;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Repositories.CRM;
using Moq;
using Xunit;

namespace FrameCraft.UnitTests.Customers.Queries;

public class GetCustomersPagedQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly IMapper _mapper;
    private readonly GetCustomersPagedQueryHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();

    public GetCustomersPagedQueryHandlerTests()
    {
        _mockCustomerRepository = new Mock<ICustomerRepository>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        _mapper = mapperConfig.CreateMapper();

        _handler = new GetCustomersPagedQueryHandler(
            _mockCustomerRepository.Object,
            _mapper);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        var customers = CreateCustomers(25);
        var pagedCustomers = customers.Take(10).ToList();

        _mockCustomerRepository
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<string>(),   // ✅ FIX
                It.IsAny<string>(),   // ✅ FIX
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((pagedCustomers, 25));

        var query = new GetCustomersPagedQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SecondPage_HasPrevious()
    {
        var customers = CreateCustomers(25);
        var pagedCustomers = customers.Skip(10).Take(10).ToList();

        _mockCustomerRepository
            .Setup(x => x.GetPagedAsync(
                10,
                10,
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((pagedCustomers, 25));

        var query = new GetCustomersPagedQuery
        {
            PageNumber = 2,
            PageSize = 10
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.HasPrevious.Should().BeTrue();
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_LastPage_NoNext()
    {
        var customers = CreateCustomers(5);

        _mockCustomerRepository
            .Setup(x => x.GetPagedAsync(
                20,
                10,
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((customers, 25));

        var query = new GetCustomersPagedQuery
        {
            PageNumber = 3,
            PageSize = 10
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithSearchFilter_PassesSearchToRepository()
    {
        var search = "Test";

        _mockCustomerRepository
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                search,
                It.IsAny<bool?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Customer>(), 0));

        var query = new GetCustomersPagedQuery { Search = search };

        await _handler.Handle(query, CancellationToken.None);

        _mockCustomerRepository.Verify(x =>
            x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                search,
                It.IsAny<bool?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSorting_PassesSortingToRepository()
    {
        _mockCustomerRepository
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                "email",
                "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Customer>(), 0));

        var query = new GetCustomersPagedQuery
        {
            SortBy = "email",
            SortOrder = "desc"
        };

        await _handler.Handle(query, CancellationToken.None);

        _mockCustomerRepository.Verify(x =>
            x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                "email",
                "desc",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private List<Customer> CreateCustomers(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                Name = $"Customer {i}",
                Email = $"customer{i}@test.com",
                IsActive = true
            })
            .ToList();
    }
}
