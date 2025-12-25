using FluentAssertions;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrameCraft.UnitTests.MultiTenant;

/// <summary>
/// User visibility testleri
/// Tenant admin SuperAdmin kullanıcıları görememeli
/// </summary>
public class UserVisibilityTests
{
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();

    [Fact]
    public async Task TenantAdmin_CannotSee_SuperAdminUsers()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantAId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        // Normal tenant user
        var normalUser = TestDataBuilder.CreateUser(_tenantAId, "normal@test.com", isSuperAdmin: false);
        
        // SuperAdmin (TenantId = Guid.Empty veya farklı)
        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty, // SuperAdmin'ler genellikle tenant'a bağlı değil
            Email = "superadmin@system.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            FirstName = "Super",
            LastName = "Admin",
            IsSuperAdmin = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(normalUser);
        dbContext.Users.Add(superAdmin);
        await dbContext.SaveChangesAsync();

        // Act - Tenant A olarak kullanıcıları sorgula
        var users = await dbContext.Users.ToListAsync();

        // Assert - SuperAdmin'i görmemeli
        users.Should().NotContain(u => u.IsSuperAdmin);
        users.Should().OnlyContain(u => u.TenantId == _tenantAId);
    }

    [Fact]
    public async Task TenantAdmin_CanSee_OnlyOwnTenantUsers()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantAId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var userA1 = TestDataBuilder.CreateUser(_tenantAId, "usera1@test.com");
        var userA2 = TestDataBuilder.CreateUser(_tenantAId, "usera2@test.com");
        var userB1 = TestDataBuilder.CreateUser(_tenantBId, "userb1@test.com");

        dbContext.Users.AddRange(userA1, userA2, userB1);
        await dbContext.SaveChangesAsync();

        // Act
        var users = await dbContext.Users.ToListAsync();

        // Assert
        users.Should().HaveCount(2);
        users.Should().OnlyContain(u => u.TenantId == _tenantAId);
        users.Should().NotContain(u => u.Email == "userb1@test.com");
    }

    [Fact]
    public async Task TenantAdmin_CanSee_InactiveUsersInOwnTenant()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantAId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var activeUser = TestDataBuilder.CreateUser(_tenantAId, "active@test.com", isActive: true);
        var inactiveUser = TestDataBuilder.CreateUser(_tenantAId, "inactive@test.com", isActive: false);

        dbContext.Users.AddRange(activeUser, inactiveUser);
        await dbContext.SaveChangesAsync();

        // Act
        var users = await dbContext.Users.ToListAsync();

        // Assert - Aktif olmayan kullanıcıları da görmeli (soft delete değil, sadece inactive)
        users.Should().HaveCount(2);
        users.Should().Contain(u => u.Email == "active@test.com");
        users.Should().Contain(u => u.Email == "inactive@test.com");
    }

    [Fact]
    public async Task SuperAdmin_CanSee_AllUsersIncludingOtherSuperAdmins()
    {
        // Arrange
        var superAdminContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(superAdminContext.Object);

        var normalUserA = TestDataBuilder.CreateUser(_tenantAId, "usera@test.com");
        var normalUserB = TestDataBuilder.CreateUser(_tenantBId, "userb@test.com");
        var superAdmin1 = new User
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            Email = "superadmin1@system.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            FirstName = "Super",
            LastName = "Admin1",
            IsSuperAdmin = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.AddRange(normalUserA, normalUserB, superAdmin1);
        await dbContext.SaveChangesAsync();

        // Act
        var users = await dbContext.Users.ToListAsync();

        // Assert - Tüm kullanıcıları görmeli
        users.Should().HaveCount(3);
        users.Should().Contain(u => u.IsSuperAdmin);
    }

    [Fact]
    public async Task TenantUser_CannotSee_DeletedUsers()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantAId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var activeUser = TestDataBuilder.CreateUser(_tenantAId, "active@test.com");
        var deletedUser = TestDataBuilder.CreateUser(_tenantAId, "deleted@test.com");
        deletedUser.IsDeleted = true;

        dbContext.Users.AddRange(activeUser, deletedUser);
        await dbContext.SaveChangesAsync();

        // Act
        var users = await dbContext.Users.ToListAsync();

        // Assert
        users.Should().HaveCount(1);
        users.Should().NotContain(u => u.IsDeleted);
    }

    [Fact]
    public async Task CountUsers_RespectsTenanFiltering()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantAId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        // Tenant A: 3 users, Tenant B: 2 users
        dbContext.Users.AddRange(
            TestDataBuilder.CreateUser(_tenantAId, "a1@test.com"),
            TestDataBuilder.CreateUser(_tenantAId, "a2@test.com"),
            TestDataBuilder.CreateUser(_tenantAId, "a3@test.com"),
            TestDataBuilder.CreateUser(_tenantBId, "b1@test.com"),
            TestDataBuilder.CreateUser(_tenantBId, "b2@test.com"));
        await dbContext.SaveChangesAsync();

        // Act
        var count = await dbContext.Users.CountAsync();

        // Assert - Sadece Tenant A'nın 3 kullanıcısını saymalı
        count.Should().Be(3);
    }
}
