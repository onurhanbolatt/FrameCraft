using AutoMapper;
using FluentAssertions;
using FrameCraft.Application.Common.Mappings;
using FrameCraft.Application.Customers.Queries.GetCustomerById;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Repositories.CRM;
using Moq;
using Xunit;

namespace FrameCraft.UnitTests.Customers.Queries;

public class GetCustomerByIdQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly IMapper _mapper;
    private readonly GetCustomerByIdQueryHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();

    public GetCustomerByIdQueryHandlerTests()
    {
        _mockCustomerRepository = new Mock<ICustomerRepository>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        _handler = new GetCustomerByIdQueryHandler(
            _mockCustomerRepository.Object,
            _mapper);
    }

    [Fact]
    public async Task Handle_ExistingCustomer_ReturnsCustomerDto()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer
        {
            Id = customerId,
            TenantId = _tenantId,
            Name = "Test Customer",
            Email = "test@test.com",
            Phone = "555-1234",
            Address = "Test Address",
            Notes = "Test Notes",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockCustomerRepository
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var query = new GetCustomerByIdQuery(customerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(customerId);
        result.Name.Should().Be("Test Customer");
        result.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task Handle_NonExistentCustomer_ReturnsNull()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _mockCustomerRepository
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var query = new GetCustomerByIdQuery(customerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeletedCustomer_ReturnsNull()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var deletedCustomer = new Customer
        {
            Id = customerId,
            TenantId = _tenantId,
            Name = "Deleted Customer",
            IsDeleted = true
        };

        _mockCustomerRepository
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null); // Repository returns null for deleted

        var query = new GetCustomerByIdQuery(customerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CustomerWithAllFields_MapsCorrectly()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-10);
        var updatedAt = DateTime.UtcNow.AddDays(-1);

        var customer = new Customer
        {
            Id = customerId,
            TenantId = _tenantId,
            Name = "Full Customer",
            Email = "full@test.com",
            Phone = "555-9999",
            Address = "Full Address Line 1\nLine 2",
            Notes = "Important customer notes",
            IsActive = false,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        _mockCustomerRepository
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var query = new GetCustomerByIdQuery(customerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Full Customer");
        result.Email.Should().Be("full@test.com");
        result.Phone.Should().Be("555-9999");
        result.Address.Should().Contain("Full Address");
        result.Notes.Should().Be("Important customer notes");
        result.IsActive.Should().BeFalse();
    }
}
