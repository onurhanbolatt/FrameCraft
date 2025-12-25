using AutoMapper;
using FrameCraft.Application.Common.Mappings;
using FrameCraft.Application.Customers.Queries.GetCustomers;
using FrameCraft.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Repositories.CRM;

namespace FrameCraft.UnitTests.Customers.Queries;

public class GetCustomersQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly IMapper _mapper;
    private readonly GetCustomersQueryHandler _handler;

    public GetCustomersQueryHandlerTests()
    {
        _mockCustomerRepository = new Mock<ICustomerRepository>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        _handler = new GetCustomersQueryHandler(
            _mockCustomerRepository.Object,
            _mapper
        );
    }

    [Fact]
    public async Task Handle_ReturnsCustomerList()
    {
        // Arrange
        var customers = new List<Customer>
        {
            new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Name = "Ahmet Yılmaz",
                Email = "ahmet@test.com",
                Phone = "05551234567",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Name = "Ayşe Demir",
                Email = "ayse@test.com",
                Phone = "05559876543",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockCustomerRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        var query = new GetCustomersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Ahmet Yılmaz");
        result[1].Name.Should().Be("Ayşe Demir");

        _mockCustomerRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        _mockCustomerRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>());

        var query = new GetCustomersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
