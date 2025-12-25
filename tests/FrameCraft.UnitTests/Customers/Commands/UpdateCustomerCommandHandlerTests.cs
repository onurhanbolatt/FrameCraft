using AutoMapper;
using FrameCraft.Application.Common.Mappings;
using FrameCraft.Application.Customers.Commands.UpdateCustomer;
using FrameCraft.Domain.Entities;
using FrameCraft.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Repositories.CRM;

namespace FrameCraft.UnitTests.Customers.Commands;

public class UpdateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly Mock<ILogger<UpdateCustomerCommandHandler>> _mockLogger;
    private readonly IMapper _mapper;
    private readonly UpdateCustomerCommandHandler _handler;

    public UpdateCustomerCommandHandlerTests()
    {
        _mockCustomerRepository = new Mock<ICustomerRepository>();
        _mockLogger = new Mock<ILogger<UpdateCustomerCommandHandler>>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        _handler = new UpdateCustomerCommandHandler(
            _mockCustomerRepository.Object,
            _mapper,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsUpdatedCustomerDto()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = new Customer
        {
            Id = customerId,
            TenantId = Guid.NewGuid(),
            Name = "Old Name",
            Email = "old@test.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var command = new UpdateCustomerCommand
        {
            Id = customerId,
            Name = "New Name",
            Email = "new@test.com",
            Phone = "05559999999",
            IsActive = false
        };

        _mockCustomerRepository
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        _mockCustomerRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockCustomerRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        result.Email.Should().Be("new@test.com");
        result.Phone.Should().Be("05559999999");
        result.IsActive.Should().BeFalse();
        result.UpdatedAt.Should().NotBeNull();

        _mockCustomerRepository.Verify(x => x.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCustomerRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new UpdateCustomerCommand
        {
            Id = customerId,
            Name = "Test"
        };

        _mockCustomerRepository
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _mockCustomerRepository.Verify(x => x.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesTimestamp()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = new Customer
        {
            Id = customerId,
            TenantId = Guid.NewGuid(),
            Name = "Test",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = null
        };

        var command = new UpdateCustomerCommand
        {
            Id = customerId,
            Name = "Updated Name"
        };

        _mockCustomerRepository
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        _mockCustomerRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        existingCustomer.UpdatedAt.Should().NotBeNull();
        existingCustomer.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
