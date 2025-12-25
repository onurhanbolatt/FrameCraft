using FluentAssertions;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Users.Commands.CreateUser;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Entities.Core;
using FrameCraft.Domain.Enums;
using FrameCraft.Domain.Exceptions;
using FrameCraft.Domain.Repositories.Authentication;
using FrameCraft.Domain.Repositories.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FrameCraft.UnitTests.Users.Commands;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ILogger<CreateUserCommandHandler>> _mockLogger;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly CreateUserCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateUserCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<CreateUserCommandHandler>>();

        // Handler SuperAdmin kontrolü yaptığı için default true
        _mockTenantContext.SetupGet(x => x.IsSuperAdmin).Returns(true);

        _handler = new CreateUserCommandHandler(
            _mockUserRepository.Object,
            _mockTenantRepository.Object,
            _mockRoleRepository.Object,
            _mockPasswordHasher.Object,
            _mockTenantContext.Object,
            _mockLogger.Object);
    }


    [Fact]
    public async Task Handle_ValidCommand_CreatesUser()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            TenantId = _tenantId,
            Email = "newuser@test.com",
            Password = "Test123!",
            FirstName = "New",
            LastName = "User",
            Roles = new List<string> { "User" }
        };

        var tenant = new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            MaxUsers = 10,
            Users = new List<User>()
        };

        var userRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "User",
            Description = "Standard User"
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashedPassword");

        _mockRoleRepository
            .Setup(x => x.GetByNameAsync("User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRole);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("newuser@test.com");
        result.TenantId.Should().Be(_tenantId);
        result.Roles.Should().Contain("User");

        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidTenant_ThrowsNotFoundException()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            TenantId = Guid.NewGuid(),
            Email = "newuser@test.com",
            Password = "Test123!",
            FirstName = "New",
            LastName = "User"
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(command.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DeletedTenant_ThrowsNotFoundException()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            TenantId = _tenantId,
            Email = "newuser@test.com",
            Password = "Test123!"
        };

        var deletedTenant = new Tenant
        {
            Id = _tenantId,
            Name = "Deleted Tenant",
            IsDeleted = true
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedTenant);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsBadRequestException()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            TenantId = _tenantId,
            Email = "existing@test.com",
            Password = "Test123!",
            FirstName = "New",
            LastName = "User"
        };

        var tenant = new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            MaxUsers = 10,
            Users = new List<User>()
        };

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@test.com"
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("zaten kullanılıyor");
    }

    [Fact]
    public async Task Handle_TenantUserLimitReached_ThrowsBadRequestException()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            TenantId = _tenantId,
            Email = "newuser@test.com",
            Password = "Test123!"
        };

        var tenant = new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            MaxUsers = 2, // Limit 2
            Users = new List<User>
            {
                new User { Id = Guid.NewGuid(), IsDeleted = false },
                new User { Id = Guid.NewGuid(), IsDeleted = false }
            }
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("limitine");
    }

    [Fact]
    public async Task Handle_DeletedUsersNotCountedInLimit()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            TenantId = _tenantId,
            Email = "newuser@test.com",
            Password = "Test123!",
            FirstName = "New",
            LastName = "User",
            Roles = new List<string> { "User" }
        };

        var tenant = new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            MaxUsers = 2, // Limit 2
            Users = new List<User>
            {
                new User { Id = Guid.NewGuid(), IsDeleted = false },
                new User { Id = Guid.NewGuid(), IsDeleted = true } // Deleted, doesn't count
            }
        };

        var userRole = new Role { Id = Guid.NewGuid(), Name = "User" };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hash");

        _mockRoleRepository
            .Setup(x => x.GetByNameAsync("User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRole);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Should succeed because deleted users don't count
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_CreatesRoleIfNotExists()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            TenantId = _tenantId,
            Email = "newuser@test.com",
            Password = "Test123!",
            FirstName = "New",
            LastName = "User",
            Roles = new List<string> { "NewRole" }
        };

        var tenant = new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            MaxUsers = 10,
            Users = new List<User>()
        };

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hash");

        _mockRoleRepository
            .Setup(x => x.GetByNameAsync("NewRole", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null); // Role doesn't exist

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Role should be created
        _mockRoleRepository.Verify(
            x => x.AddAsync(It.Is<Role>(r => r.Name == "NewRole"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmailConvertedToLowercase()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            TenantId = _tenantId,
            Email = "TestUser@TEST.COM",
            Password = "Test123!",
            FirstName = "Test",
            LastName = "User",
            Roles = new List<string>()
        };

        var tenant = new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            MaxUsers = 10,
            Users = new List<User>()
        };

        User? capturedUser = null;

        _mockTenantRepository
            .Setup(x => x.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hash");

        _mockUserRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .ReturnsAsync((User u, CancellationToken _) => u);


        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Email.Should().Be("testuser@test.com");
    }
}
