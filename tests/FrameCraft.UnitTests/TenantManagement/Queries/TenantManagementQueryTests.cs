using FluentAssertions;
using FrameCraft.Application.TenantManagement.Queries;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrameCraft.UnitTests.TenantManagement.Queries;

public class GetTenantUsersQueryTests
{
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ReturnsOnlyTenantUsers()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        // Add users for different tenants
        var userA1 = TestDataBuilder.CreateUser(_tenantAId, "usera1@test.com");
        var userA2 = TestDataBuilder.CreateUser(_tenantAId, "usera2@test.com");
        var userB1 = TestDataBuilder.CreateUser(_tenantBId, "userb1@test.com");

        dbContext.Users.AddRange(userA1, userA2, userB1);
        await dbContext.SaveChangesAsync();

        var handler = new GetTenantUsersQueryHandler(dbContext);
        var query = new GetTenantUsersQuery(_tenantAId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(u => u.Email.StartsWith("usera"));
    }

    [Fact]
    public async Task Handle_ExcludesSuperAdmins()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var normalUser = TestDataBuilder.CreateUser(_tenantAId, "normal@test.com", isSuperAdmin: false);
        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantAId, // Even in same tenant
            Email = "super@test.com",
            PasswordHash = "hash",
            FirstName = "Super",
            LastName = "Admin",
            IsSuperAdmin = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.AddRange(normalUser, superAdmin);
        await dbContext.SaveChangesAsync();

        var handler = new GetTenantUsersQueryHandler(dbContext);
        var query = new GetTenantUsersQuery(_tenantAId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Should().NotContain(u => u.Email == "super@test.com");
    }

    [Fact]
    public async Task Handle_ReturnsEmptyListForEmptyTenant()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var handler = new GetTenantUsersQueryHandler(dbContext);
        var query = new GetTenantUsersQuery(Guid.NewGuid()); // Non-existent tenant

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OrdersByCreatedAtDescending()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var user1 = TestDataBuilder.CreateUser(_tenantAId, "first@test.com");
        user1.CreatedAt = DateTime.UtcNow.AddDays(-2);

        var user2 = TestDataBuilder.CreateUser(_tenantAId, "second@test.com");
        user2.CreatedAt = DateTime.UtcNow.AddDays(-1);

        var user3 = TestDataBuilder.CreateUser(_tenantAId, "third@test.com");
        user3.CreatedAt = DateTime.UtcNow;

        dbContext.Users.AddRange(user1, user2, user3);
        await dbContext.SaveChangesAsync();

        var handler = new GetTenantUsersQueryHandler(dbContext);
        var query = new GetTenantUsersQuery(_tenantAId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].Email.Should().Be("third@test.com");
        result[1].Email.Should().Be("second@test.com");
        result[2].Email.Should().Be("first@test.com");
    }
}

public class GetTenantUserByIdQueryTests
{
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidUserAndTenant_ReturnsUser()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var user = TestDataBuilder.CreateUser(_tenantAId, "user@test.com");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var handler = new GetTenantUserByIdQueryHandler(dbContext);
        var query = new GetTenantUserByIdQuery(user.Id, _tenantAId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task Handle_UserInDifferentTenant_ReturnsNull()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var user = TestDataBuilder.CreateUser(_tenantAId, "user@test.com");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var handler = new GetTenantUserByIdQueryHandler(dbContext);
        var query = new GetTenantUserByIdQuery(user.Id, _tenantBId); // Different tenant

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull("User Tenant B'de değil");
    }

    [Fact]
    public async Task Handle_SuperAdminUser_ReturnsNull()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantAId,
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

        var handler = new GetTenantUserByIdQueryHandler(dbContext);
        var query = new GetTenantUserByIdQuery(superAdmin.Id, _tenantAId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull("SuperAdmin kullanıcılar tenant listesinde görünmez");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var handler = new GetTenantUserByIdQueryHandler(dbContext);
        var query = new GetTenantUserByIdQuery(Guid.NewGuid(), _tenantAId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}

public class GetTenantInfoQueryTests
{
    [Fact]
    public async Task Handle_ExistingTenant_ReturnsTenantInfo()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var tenant = TestDataBuilder.CreateTenant("Test Company", "test-company");
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var handler = new GetTenantInfoQueryHandler(dbContext);
        var query = new GetTenantInfoQuery(tenant.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Company");
        result.Subdomain.Should().Be("test-company");
    }

    [Fact]
    public async Task Handle_TenantWithUsers_ReturnsCorrectUserCount()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var tenant = TestDataBuilder.CreateTenant("Test Company");
        dbContext.Tenants.Add(tenant);

        // Add 3 users to tenant
        var users = TestDataBuilder.CreateCustomers(tenant.Id, 3);
        var user1 = TestDataBuilder.CreateUser(tenant.Id, "user1@test.com");
        var user2 = TestDataBuilder.CreateUser(tenant.Id, "user2@test.com");
        var user3 = TestDataBuilder.CreateUser(tenant.Id, "user3@test.com");
        dbContext.Users.AddRange(user1, user2, user3);
        await dbContext.SaveChangesAsync();

        var handler = new GetTenantInfoQueryHandler(dbContext);
        var query = new GetTenantInfoQuery(tenant.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.CurrentUserCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_TenantWithDeletedUsers_ExcludesDeletedFromCount()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var tenant = TestDataBuilder.CreateTenant("Test Company");
        dbContext.Tenants.Add(tenant);

        var activeUser = TestDataBuilder.CreateUser(tenant.Id, "active@test.com");
        var deletedUser = TestDataBuilder.CreateUser(tenant.Id, "deleted@test.com");
        deletedUser.IsDeleted = true;

        dbContext.Users.AddRange(activeUser, deletedUser);
        await dbContext.SaveChangesAsync();

        var handler = new GetTenantInfoQueryHandler(dbContext);
        var query = new GetTenantInfoQuery(tenant.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.CurrentUserCount.Should().Be(1, "Silinen kullanıcılar sayılmamalı");
    }

    [Fact]
    public async Task Handle_NonExistentTenant_ReturnsNull()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var handler = new GetTenantInfoQueryHandler(dbContext);
        var query = new GetTenantInfoQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
