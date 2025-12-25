using AutoMapper;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Common.Mappings;
using FrameCraft.Application.Customers.Commands.CreateCustomer;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FrameCraft.Domain.Repositories.CRM;

namespace FrameCraft.UnitTests.Customers.Commands
{
    public class CreateCustomerCommandHandlerTests
    {
        private readonly Mock<ICustomerRepository> _mockCustomerRepository;
        private readonly Mock<ITenantContext> _mockTenantContext;
        private readonly Mock<ILogger<CreateCustomerCommandHandler>> _mockLogger;
        private readonly IMapper _mapper;
        private readonly CreateCustomerCommandHandler _handler;

        private readonly Guid _tenantId = Guid.NewGuid();

        public CreateCustomerCommandHandlerTests()
        {
            _mockCustomerRepository = new Mock<ICustomerRepository>();
            _mockTenantContext = new Mock<ITenantContext>();
            _mockLogger = new Mock<ILogger<CreateCustomerCommandHandler>>();

            // Tenant context – başarılı senaryolar için default değer
            _mockTenantContext
                .Setup(x => x.CurrentTenantId)
                .Returns(_tenantId);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = mapperConfig.CreateMapper();

            _handler = new CreateCustomerCommandHandler(
                _mockCustomerRepository.Object,
                _mockTenantContext.Object,
                _mapper,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsCustomerId()
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "Ahmet Yılmaz",
                Email = "ahmet@test.com",
                Phone = "05551234567",
                IsActive = true
            };

            _mockCustomerRepository
                .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer c, CancellationToken _) => c);

            _mockCustomerRepository
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var customerId = await _handler.Handle(command, CancellationToken.None);

            // Assert
            customerId.Should().NotBeEmpty();
            _mockCustomerRepository.Verify(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockCustomerRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_TenantNotFound_ThrowsBadRequestException()
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "Ahmet Yılmaz",
                Email = "ahmet@test.com"
            };

            // Tenant yokmuş gibi davransın
            _mockTenantContext
                .Setup(x => x.CurrentTenantId)
                .Returns((Guid?)null);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => _handler.Handle(command, CancellationToken.None));

            _mockCustomerRepository.Verify(
                x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ValidCommand_SetsCorrectProperties()
        {
            // Arrange
            var command = new CreateCustomerCommand
            {
                Name = "Test Customer",
                Address = "Test Address",
                Notes = "Test Notes",
                IsActive = true
            };

            Customer? capturedCustomer = null;

            _mockCustomerRepository
                .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .Callback<Customer, CancellationToken>((c, _) => capturedCustomer = c)
                .ReturnsAsync((Customer c, CancellationToken _) => c);

            _mockCustomerRepository
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var customerId = await _handler.Handle(command, CancellationToken.None);

            // Assert
            customerId.Should().NotBeEmpty();

            capturedCustomer.Should().NotBeNull();
            capturedCustomer!.Id.Should().Be(customerId);
            capturedCustomer.TenantId.Should().Be(_tenantId);
            capturedCustomer.Name.Should().Be(command.Name);
            capturedCustomer.Address.Should().Be(command.Address);
            capturedCustomer.Notes.Should().Be(command.Notes);
            capturedCustomer.IsDeleted.Should().BeFalse();
            capturedCustomer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }
}
