using FluentAssertions;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Tenants.Commands.CreateTenant;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Entities.Core;
using FrameCraft.Domain.Enums;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Authentication;
using FrameCraft.Domain.Repositories.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FrameCraft.UnitTests.Tenants.Commands;

public class CreateTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ILogger<CreateTenantCommandHandler>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly CreateTenantCommandHandler _handler;

    public CreateTenantCommandHandlerTests()
    {
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<CreateTenantCommandHandler>>();

        // Handler SuperAdmin kontrolü yaptığı için testlerde default true yap
        _mockTenantContext.SetupGet(x => x.IsSuperAdmin).Returns(true);

        _handler = new CreateTenantCommandHandler(
            _mockTenantRepository.Object,
            _mockUserRepository.Object,
            _mockRoleRepository.Object,
            _mockPasswordHasher.Object,
            _mockTenantContext.Object,
            _mockLogger.Object);
    }


    [Fact]
    public async Task Handle_ValidCommand_CreatesTenant()
    {
        // Arrange
        var command = new CreateTenantCommand
        {
            Name = "New Company",
            Subdomain = "new-company",
            Phone = "555-1234",
            Email = "info@newcompany.com",
            MaxUsers = 10
        };

        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync(command.Subdomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Company");
        result.Subdomain.Should().Be("new-company");
        result.Status.Should().Be(TenantStatus.Active);

        _mockTenantRepository.Verify(
            x => x.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateSubdomain_ThrowsBadRequestException()
    {
        // Arrange
        var command = new CreateTenantCommand
        {
            Name = "New Company",
            Subdomain = "existing-subdomain"
        };

        var existingTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Existing Company",
            Subdomain = "existing-subdomain"
        };

        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync(command.Subdomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTenant);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("zaten kullanılıyor");
    }

    [Fact]
    public async Task Handle_WithAdminUser_CreatesAdminUser()
    {
        // Arrange
        var command = new CreateTenantCommand
        {
            Name = "New Company",
            Subdomain = "new-company",
            AdminEmail = "admin@newcompany.com",
            AdminPassword = "Admin123!",
            AdminFirstName = "John",
            AdminLastName = "Doe"
        };

        var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };

        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync(command.Subdomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(command.AdminEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockRoleRepository
            .Setup(x => x.GetByNameAsync("Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminRole);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(command.AdminPassword))
            .Returns("hashedPassword");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserCount.Should().Be(1);

        _mockUserRepository.Verify(
            x => x.AddAsync(It.Is<User>(u =>
                u.Email == "admin@newcompany.com" &&
                u.FirstName == "John" &&
                u.LastName == "Doe"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateAdminEmail_ThrowsBadRequestException()
    {
        // Arrange
        var command = new CreateTenantCommand
        {
            Name = "New Company",
            Subdomain = "new-company",
            AdminEmail = "existing@test.com",
            AdminPassword = "Test123!"
        };

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@test.com"
        };

        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync(command.Subdomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(command.AdminEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("e-posta adresi zaten kullanılıyor");
    }

    [Fact]
    public async Task Handle_CreatesAdminRoleIfNotExists()
    {
        // Arrange
        var command = new CreateTenantCommand
        {
            Name = "New Company",
            Subdomain = "new-company",
            AdminEmail = "admin@newcompany.com",
            AdminPassword = "Admin123!"
        };

        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync(command.Subdomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(command.AdminEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockRoleRepository
            .Setup(x => x.GetByNameAsync("Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null); // Role doesn't exist

        _mockPasswordHasher
            .Setup(x => x.HashPassword(command.AdminPassword))
            .Returns("hash");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockRoleRepository.Verify(
            x => x.AddAsync(It.Is<Role>(r => r.Name == "Admin"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutAdminUser_ReturnsZeroUserCount()
    {
        // Arrange
        var command = new CreateTenantCommand
        {
            Name = "New Company",
            Subdomain = "new-company"
            // No admin user info
        };

        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync(command.Subdomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.UserCount.Should().Be(0);

        _mockUserRepository.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SubdomainConvertedToLowercase()
    {
        // Arrange
        var command = new CreateTenantCommand
        {
            Name = "New Company",
            Subdomain = "NEW-COMPANY" // Uppercase
        };

        Tenant? capturedTenant = null;

        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync(command.Subdomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        _mockTenantRepository
            .Setup(x => x.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Callback<Tenant, CancellationToken>((t, _) => capturedTenant = t)
            .ReturnsAsync((Tenant t, CancellationToken _) => t);


        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedTenant.Should().NotBeNull();
        capturedTenant!.Subdomain.Should().Be("new-company");
    }

    [Fact]
    public async Task Handle_DefaultSubscriptionPlan_IsBasic()
    {
        // Arrange
        var command = new CreateTenantCommand
        {
            Name = "New Company",
            Subdomain = "new-company"
            // No SubscriptionPlan specified
        };

        Tenant? capturedTenant = null;

        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync(command.Subdomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        _mockTenantRepository
            .Setup(x => x.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Callback<Tenant, CancellationToken>((t, _) => capturedTenant = t)
            .ReturnsAsync((Tenant t, CancellationToken _) => t);


        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedTenant!.SubscriptionPlan.Should().Be("Basic");
    }
}
