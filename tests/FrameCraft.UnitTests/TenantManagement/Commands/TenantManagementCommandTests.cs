using FluentAssertions;
using FluentValidation.TestHelper;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.TenantManagement.Commands;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Entities.Core;
using FrameCraft.Domain.Enums;
using FrameCraft.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FrameCraft.UnitTests.TenantManagement.Commands;

public class CreateTenantUserCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidCommand_CreatesUser()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockLogger = new Mock<ILogger<CreateTenantUserCommandHandler>>();

        // Seed tenant and role
        var tenant = TestDataBuilder.CreateTenant("Test Tenant");
        tenant.Id = _tenantId;
        dbContext.Tenants.Add(tenant);

        var userRole = TestDataBuilder.CreateRole("User", "Standard User");
        dbContext.Roles.Add(userRole);
        await dbContext.SaveChangesAsync();

        var handler = new CreateTenantUserCommandHandler(dbContext, mockLogger.Object);

        var command = new CreateTenantUserCommand
        {
            TenantId = _tenantId,
            Email = "newuser@test.com",
            FirstName = "New",
            LastName = "User",
            Password = "Test123!",
            Roles = new List<string> { "User" }
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("newuser@test.com");
        result.FullName.Should().Be("New User");
        result.Roles.Should().Contain("User");

        // Verify in database
        var createdUser = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == "newuser@test.com");
        createdUser.Should().NotBeNull();
        createdUser!.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsValidationException()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockLogger = new Mock<ILogger<CreateTenantUserCommandHandler>>();

        var tenant = TestDataBuilder.CreateTenant();
        tenant.Id = _tenantId;
        dbContext.Tenants.Add(tenant);

        var existingUser = TestDataBuilder.CreateUser(_tenantId, "existing@test.com");
        dbContext.Users.Add(existingUser);
        await dbContext.SaveChangesAsync();

        var handler = new CreateTenantUserCommandHandler(dbContext, mockLogger.Object);

        var command = new CreateTenantUserCommand
        {
            TenantId = _tenantId,
            Email = "existing@test.com", // Duplicate
            FirstName = "New",
            LastName = "User",
            Password = "Test123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<FrameCraft.Domain.Exceptions.ValidationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidTenant_ThrowsNotFoundException()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockLogger = new Mock<ILogger<CreateTenantUserCommandHandler>>();

        var handler = new CreateTenantUserCommandHandler(dbContext, mockLogger.Object);

        var command = new CreateTenantUserCommand
        {
            TenantId = Guid.NewGuid(), // Non-existent tenant
            Email = "newuser@test.com",
            FirstName = "New",
            LastName = "User",
            Password = "Test123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<FrameCraft.Domain.Exceptions.NotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public void Validator_SuperAdminRole_ShouldFail()
    {
        // Arrange
        var validator = new CreateTenantUserCommandValidator();
        var command = new CreateTenantUserCommand
        {
            TenantId = _tenantId,
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User",
            Password = "Test123!",
            Roles = new List<string> { "SuperAdmin" } // Should fail
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Roles)
            .WithErrorMessage("SuperAdmin rolü atanamaz");
    }
}

public class UpdateTenantUserStatusCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _currentUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidCommand_UpdatesUserStatus()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: false);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockLogger = new Mock<ILogger<UpdateTenantUserStatusCommandHandler>>();

        var user = TestDataBuilder.CreateUser(_tenantId, "user@test.com", isActive: true);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var handler = new UpdateTenantUserStatusCommandHandler(dbContext, mockLogger.Object);

        var command = new UpdateTenantUserStatusCommand
        {
            UserId = user.Id,
            TenantId = _tenantId,
            CurrentUserId = _currentUserId,
            IsActive = false
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var updatedUser = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == user.Id);
        updatedUser!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CannotDeactivateSelf()
    {
        // Arrange
        var validator = new UpdateTenantUserStatusCommandValidator();
        var userId = Guid.NewGuid();

        var command = new UpdateTenantUserStatusCommand
        {
            UserId = userId,
            TenantId = _tenantId,
            CurrentUserId = userId, // Same as UserId
            IsActive = false
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("Kendinizi pasif yapamazsınız");
    }

    [Fact]
    public async Task Handle_SuperAdminUser_ReturnsFalse()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: false);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockLogger = new Mock<ILogger<UpdateTenantUserStatusCommandHandler>>();

        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Email = "super@test.com",
            PasswordHash = "hash",
            FirstName = "Super",
            LastName = "Admin",
            IsSuperAdmin = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(superAdmin);
        await dbContext.SaveChangesAsync();

        var handler = new UpdateTenantUserStatusCommandHandler(dbContext, mockLogger.Object);

        var command = new UpdateTenantUserStatusCommand
        {
            UserId = superAdmin.Id,
            TenantId = _tenantId,
            CurrentUserId = _currentUserId,
            IsActive = false
        };

        // Act - SuperAdmin status değiştirilemez
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse("SuperAdmin kullanıcıların durumu değiştirilemez");
    }
}

public class DeleteTenantUserCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _currentUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidCommand_SoftDeletesUser()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: false);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockLogger = new Mock<ILogger<DeleteTenantUserCommandHandler>>();

        var user = TestDataBuilder.CreateUser(_tenantId, "user@test.com");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var handler = new DeleteTenantUserCommandHandler(dbContext, mockLogger.Object);

        var command = new DeleteTenantUserCommand
        {
            UserId = user.Id,
            TenantId = _tenantId,
            CurrentUserId = _currentUserId
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var deletedUser = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == user.Id);
        deletedUser!.IsDeleted.Should().BeTrue();
        deletedUser.DeletedAt.Should().NotBeNull();
        deletedUser.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Validator_CannotDeleteSelf()
    {
        // Arrange
        var validator = new DeleteTenantUserCommandValidator();
        var userId = Guid.NewGuid();

        var command = new DeleteTenantUserCommand
        {
            UserId = userId,
            TenantId = _tenantId,
            CurrentUserId = userId // Same as UserId
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("Kendinizi silemezsiniz");
    }

    [Fact]
    public async Task Handle_SuperAdminUser_ReturnsFalse()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: false);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockLogger = new Mock<ILogger<DeleteTenantUserCommandHandler>>();

        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Email = "super@test.com",
            PasswordHash = "hash",
            FirstName = "Super",
            LastName = "Admin",
            IsSuperAdmin = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(superAdmin);
        await dbContext.SaveChangesAsync();

        var handler = new DeleteTenantUserCommandHandler(dbContext, mockLogger.Object);

        var command = new DeleteTenantUserCommand
        {
            UserId = superAdmin.Id,
            TenantId = _tenantId,
            CurrentUserId = _currentUserId
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse("SuperAdmin kullanıcılar silinemez");
    }

    [Fact]
    public async Task Handle_UserNotInTenant_ReturnsFalse()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: false);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockLogger = new Mock<ILogger<DeleteTenantUserCommandHandler>>();

        var otherTenantId = Guid.NewGuid();
        var user = TestDataBuilder.CreateUser(otherTenantId, "user@test.com");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var handler = new DeleteTenantUserCommandHandler(dbContext, mockLogger.Object);

        var command = new DeleteTenantUserCommand
        {
            UserId = user.Id,
            TenantId = _tenantId, // Farklı tenant
            CurrentUserId = _currentUserId
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse("Farklı tenant'ın kullanıcısı silinemez");
    }
}

public class ResetTenantUserPasswordCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidCommand_ResetsPassword()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: false);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockLogger = new Mock<ILogger<ResetTenantUserPasswordCommandHandler>>();

        var user = TestDataBuilder.CreateUser(_tenantId, "user@test.com");
        var oldPasswordHash = user.PasswordHash;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var handler = new ResetTenantUserPasswordCommandHandler(dbContext, mockLogger.Object);

        var command = new ResetTenantUserPasswordCommand
        {
            UserId = user.Id,
            TenantId = _tenantId,
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var updatedUser = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == user.Id);
        updatedUser!.PasswordHash.Should().NotBe(oldPasswordHash);
    }

    [Fact]
    public void Validator_PasswordMismatch_ShouldFail()
    {
        // Arrange
        var validator = new ResetTenantUserPasswordCommandValidator();
        var command = new ResetTenantUserPasswordCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = _tenantId,
            NewPassword = "Password123!",
            ConfirmPassword = "DifferentPassword!"
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Şifreler eşleşmiyor");
    }

    [Fact]
    public void Validator_ShortPassword_ShouldFail()
    {
        // Arrange
        var validator = new ResetTenantUserPasswordCommandValidator();
        var command = new ResetTenantUserPasswordCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = _tenantId,
            NewPassword = "12345", // Too short
            ConfirmPassword = "12345"
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
