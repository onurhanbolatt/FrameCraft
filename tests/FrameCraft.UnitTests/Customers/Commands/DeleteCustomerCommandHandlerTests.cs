using FrameCraft.Application.Customers.Commands.DeleteCustomer;
using FrameCraft.Domain.Entities;
using FrameCraft.Domain.Exceptions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Repositories.CRM;

namespace FrameCraft.UnitTests.Customers.Commands;

public class DeleteCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly Mock<ILogger<DeleteCustomerCommandHandler>> _mockLogger;
    private readonly DeleteCustomerCommandHandler _handler;

    public DeleteCustomerCommandHandlerTests()
    {
        _mockCustomerRepository = new Mock<ICustomerRepository>();
        _mockLogger = new Mock<ILogger<DeleteCustomerCommandHandler>>();

        _handler = new DeleteCustomerCommandHandler(
            _mockCustomerRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesCustomer()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = new Customer
        {
            Id = customerId,
            TenantId = Guid.NewGuid(),
            Name = "Test Customer",
            IsDeleted = false
        };

        var command = new DeleteCustomerCommand(customerId);

        _mockCustomerRepository
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        _mockCustomerRepository
            .Setup(x => x.DeleteAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockCustomerRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _mockCustomerRepository.Verify(x => x.DeleteAsync(existingCustomer, It.IsAny<CancellationToken>()), Times.Once);
        _mockCustomerRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new DeleteCustomerCommand(customerId);

        _mockCustomerRepository
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None)
        );

        _mockCustomerRepository.Verify(x => x.DeleteAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
