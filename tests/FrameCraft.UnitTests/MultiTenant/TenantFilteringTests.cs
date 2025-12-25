using FluentAssertions;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Infrastructure.Persistence;
using FrameCraft.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FrameCraft.UnitTests.MultiTenant;

/// <summary>
/// Multi-tenant filtering testleri
/// Bu testler sistemdeki en kritik güvenlik kontrollerini test eder
/// </summary>
public class TenantFilteringTests
{
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();

    #region Tenant A User Cannot See Tenant B Data

    [Fact]
    public async Task TenantAUser_CannotSee_TenantBCustomers()
    {
        // Arrange - Tenant A context ile DbContext oluştur
        var tenantAContext = TestDbContextFactory.CreateTenantContextMock(_tenantAId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantAContext.Object);

        // Seed data - Her iki tenant'a müşteri ekle
        var customerA1 = TestDataBuilder.CreateCustomer(_tenantAId, "Customer A1");
        var customerA2 = TestDataBuilder.CreateCustomer(_tenantAId, "Customer A2");
        var customerB1 = TestDataBuilder.CreateCustomer(_tenantBId, "Customer B1");
        var customerB2 = TestDataBuilder.CreateCustomer(_tenantBId, "Customer B2");

        // IgnoreQueryFilters ile ekleme yap (seed için)
        dbContext.Customers.Add(customerA1);
        dbContext.Customers.Add(customerA2);
        dbContext.Customers.Add(customerB1);
        dbContext.Customers.Add(customerB2);
        await dbContext.SaveChangesAsync();

        // Act - Tenant A olarak müşterileri sorgula
        var customers = await dbContext.Customers.ToListAsync();

        // Assert - Sadece Tenant A müşterilerini görmeli
        customers.Should().HaveCount(2);
        customers.Should().OnlyContain(c => c.TenantId == _tenantAId);
        customers.Should().Contain(c => c.Name == "Customer A1");
        customers.Should().Contain(c => c.Name == "Customer A2");
        customers.Should().NotContain(c => c.TenantId == _tenantBId);
    }

    [Fact]
    public async Task TenantBUser_CannotSee_TenantACustomers()
    {
        // Arrange - Tenant B context ile DbContext oluştur
        var tenantBContext = TestDbContextFactory.CreateTenantContextMock(_tenantBId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantBContext.Object);

        // Seed data
        var customerA1 = TestDataBuilder.CreateCustomer(_tenantAId, "Customer A1");
        var customerB1 = TestDataBuilder.CreateCustomer(_tenantBId, "Customer B1");

        dbContext.Customers.Add(customerA1);
        dbContext.Customers.Add(customerB1);
        await dbContext.SaveChangesAsync();

        // Act - Tenant B olarak sorgula
        var customers = await dbContext.Customers.ToListAsync();

        // Assert
        customers.Should().HaveCount(1);
        customers.Should().OnlyContain(c => c.TenantId == _tenantBId);
        customers.First().Name.Should().Be("Customer B1");
    }

    [Fact]
    public async Task TenantAUser_CannotSee_TenantBUsers()
    {
        // Arrange
        var tenantAContext = TestDbContextFactory.CreateTenantContextMock(_tenantAId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantAContext.Object);

        // Seed users
        var userA = TestDataBuilder.CreateUser(_tenantAId, "usera@test.com");
        var userB = TestDataBuilder.CreateUser(_tenantBId, "userb@test.com");

        dbContext.Users.Add(userA);
        dbContext.Users.Add(userB);
        await dbContext.SaveChangesAsync();

        // Act
        var users = await dbContext.Users.ToListAsync();

        // Assert
        users.Should().HaveCount(1);
        users.Should().OnlyContain(u => u.TenantId == _tenantAId);
    }

    [Fact]
    public async Task TenantAUser_CannotSee_TenantBFiles()
    {
        // Arrange
        var tenantAContext = TestDbContextFactory.CreateTenantContextMock(_tenantAId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantAContext.Object);

        // Seed files
        var fileA = TestDataBuilder.CreateUploadedFile(_tenantAId, "fileA.pdf");
        fileA.TenantId = _tenantAId;
        var fileB = TestDataBuilder.CreateUploadedFile(_tenantBId, "fileB.pdf");
        fileB.TenantId = _tenantBId;

        dbContext.UploadedFiles.Add(fileA);
        dbContext.UploadedFiles.Add(fileB);
        await dbContext.SaveChangesAsync();

        // Act
        var files = await dbContext.UploadedFiles.ToListAsync();

        // Assert
        files.Should().HaveCount(1);
        files.Should().OnlyContain(f => f.TenantId == _tenantAId);
    }

    #endregion

    #region SuperAdmin Can See All Tenants Data (Filtering Disabled)

    [Fact]
    public async Task SuperAdmin_FilteringDisabled_CanSeeAllTenantCustomers()
    {
        // Arrange - SuperAdmin context (filtering kapalı)
        var superAdminContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(superAdminContext.Object);

        // Seed data
        var customerA = TestDataBuilder.CreateCustomer(_tenantAId, "Customer A");
        var customerB = TestDataBuilder.CreateCustomer(_tenantBId, "Customer B");

        dbContext.Customers.Add(customerA);
        dbContext.Customers.Add(customerB);
        await dbContext.SaveChangesAsync();

        // Act
        var customers = await dbContext.Customers.ToListAsync();

        // Assert - Tüm tenant'ların müşterilerini görmeli
        customers.Should().HaveCount(2);
        customers.Should().Contain(c => c.TenantId == _tenantAId);
        customers.Should().Contain(c => c.TenantId == _tenantBId);
    }

    [Fact]
    public async Task SuperAdmin_FilteringDisabled_CanSeeAllTenantUsers()
    {
        // Arrange
        var superAdminContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(superAdminContext.Object);

        // Seed users
        var userA = TestDataBuilder.CreateUser(_tenantAId, "usera@test.com");
        var userB = TestDataBuilder.CreateUser(_tenantBId, "userb@test.com");

        dbContext.Users.Add(userA);
        dbContext.Users.Add(userB);
        await dbContext.SaveChangesAsync();

        // Act
        var users = await dbContext.Users.ToListAsync();

        // Assert
        users.Should().HaveCount(2);
        users.Should().Contain(u => u.TenantId == _tenantAId);
        users.Should().Contain(u => u.TenantId == _tenantBId);
    }

    [Fact]
    public async Task SuperAdmin_FilteringDisabled_CanSeeAllTenants()
    {
        // Arrange
        var superAdminContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var dbContext = TestDbContextFactory.Create(superAdminContext.Object);

        // Seed tenants
        var tenantA = TestDataBuilder.CreateTenant("Tenant A", "tenant-a");
        tenantA.Id = _tenantAId;
        var tenantB = TestDataBuilder.CreateTenant("Tenant B", "tenant-b");
        tenantB.Id = _tenantBId;

        dbContext.Tenants.Add(tenantA);
        dbContext.Tenants.Add(tenantB);
        await dbContext.SaveChangesAsync();

        // Act
        var tenants = await dbContext.Tenants.ToListAsync();

        // Assert
        tenants.Should().HaveCount(2);
    }

    #endregion

    #region SuperAdmin Switch to Tenant - Can See Only That Tenant

    [Fact]
    public async Task SuperAdmin_SwitchToTenantA_CanSeeOnlyTenantACustomers()
    {
        // Arrange - SuperAdmin Tenant A'ya switch yapmış (filtering açık, tenantId = A)
        var switchedContext = TestDbContextFactory.CreateTenantContextMock(_tenantAId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(switchedContext.Object);

        // Seed data
        var customerA = TestDataBuilder.CreateCustomer(_tenantAId, "Customer A");
        var customerB = TestDataBuilder.CreateCustomer(_tenantBId, "Customer B");

        dbContext.Customers.Add(customerA);
        dbContext.Customers.Add(customerB);
        await dbContext.SaveChangesAsync();

        // Act
        var customers = await dbContext.Customers.ToListAsync();

        // Assert - Sadece Tenant A müşterilerini görmeli
        customers.Should().HaveCount(1);
        customers.Should().OnlyContain(c => c.TenantId == _tenantAId);
    }

    [Fact]
    public async Task SuperAdmin_SwitchToTenantB_CanSeeOnlyTenantBUsers()
    {
        // Arrange - SuperAdmin Tenant B'ye switch yapmış
        var switchedContext = TestDbContextFactory.CreateTenantContextMock(_tenantBId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(switchedContext.Object);

        // Seed data
        var userA = TestDataBuilder.CreateUser(_tenantAId, "usera@test.com");
        var userB = TestDataBuilder.CreateUser(_tenantBId, "userb@test.com");

        dbContext.Users.Add(userA);
        dbContext.Users.Add(userB);
        await dbContext.SaveChangesAsync();

        // Act
        var users = await dbContext.Users.ToListAsync();

        // Assert
        users.Should().HaveCount(1);
        users.Should().OnlyContain(u => u.TenantId == _tenantBId);
    }

    #endregion

    #region Create Without Tenant - Should Throw Exception

    [Fact]
    public async Task CreateCustomer_WithoutTenantContext_Throws()
    {
        // Arrange
        var noTenantContext = TestDbContextFactory.CreateNoTenantContextMock();
        using var dbContext = TestDbContextFactory.Create(noTenantContext.Object);

        var customer = TestDataBuilder.CreateCustomer(Guid.Empty, "Test Customer");
        // 👆 KRİTİK: TenantId = Guid.Empty

        dbContext.Customers.Add(customer);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dbContext.SaveChangesAsync());

        ex.Message.Should().Contain("TenantId is required");
    }

    [Fact]
    public async Task CreateUser_WithoutTenantContext_Throws()
    {
        // Arrange
        var noTenantContext = TestDbContextFactory.CreateNoTenantContextMock();
        using var dbContext = TestDbContextFactory.Create(noTenantContext.Object);

        var user = TestDataBuilder.CreateUser(Guid.Empty, "test@test.com");
        // 👆 KRİTİK

        dbContext.Users.Add(user);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dbContext.SaveChangesAsync());

        ex.Message.Should().Contain("TenantId is required");
    }


    #endregion

    #region Soft Delete Filtering

    [Fact]
    public async Task SoftDeletedCustomer_IsNotVisible()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantAId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var activeCustomer = TestDataBuilder.CreateCustomer(_tenantAId, "Active Customer");
        var deletedCustomer = TestDataBuilder.CreateCustomer(_tenantAId, "Deleted Customer");
        deletedCustomer.IsDeleted = true;

        dbContext.Customers.Add(activeCustomer);
        dbContext.Customers.Add(deletedCustomer);
        await dbContext.SaveChangesAsync();

        // Act
        var customers = await dbContext.Customers.ToListAsync();

        // Assert
        customers.Should().HaveCount(1);
        customers.Should().OnlyContain(c => c.Name == "Active Customer");
    }

    [Fact]
    public async Task SoftDeletedUser_IsNotVisible()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantAId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var activeUser = TestDataBuilder.CreateUser(_tenantAId, "active@test.com");
        var deletedUser = TestDataBuilder.CreateUser(_tenantAId, "deleted@test.com");
        deletedUser.IsDeleted = true;

        dbContext.Users.Add(activeUser);
        dbContext.Users.Add(deletedUser);
        await dbContext.SaveChangesAsync();

        // Act
        var users = await dbContext.Users.ToListAsync();

        // Assert
        users.Should().HaveCount(1);
        users.Should().OnlyContain(u => u.Email == "active@test.com");
    }

    #endregion

    #region Cross-Tenant Data Access Prevention

    [Fact]
    public async Task TenantAUser_CannotUpdate_TenantBCustomer()
    {
        // Arrange - İlk önce SuperAdmin olarak veri ekle
        var superAdminContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var seedContext = TestDbContextFactory.Create(superAdminContext.Object);

        var customerB = TestDataBuilder.CreateCustomer(_tenantBId, "Customer B");
        seedContext.Customers.Add(customerB);
        await seedContext.SaveChangesAsync();
        var customerBId = customerB.Id;

        // Şimdi Tenant A olarak erişmeye çalış
        var tenantAContext = TestDbContextFactory.CreateTenantContextMock(_tenantAId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantAContext.Object);

        // Act - Tenant B müşterisini bulmaya çalış
        var foundCustomer = await dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == customerBId);

        // Assert - Bulamayacak çünkü farklı tenant'a ait
        foundCustomer.Should().BeNull();
    }

    [Fact]
    public async Task TenantAUser_CannotDelete_TenantBCustomer()
    {
        // Arrange
        var superAdminContext = TestDbContextFactory.CreateSuperAdminContextMock();
        using var seedContext = TestDbContextFactory.Create(superAdminContext.Object);

        var customerB = TestDataBuilder.CreateCustomer(_tenantBId, "Customer B");
        seedContext.Customers.Add(customerB);
        await seedContext.SaveChangesAsync();
        var customerBId = customerB.Id;

        // Tenant A context
        var tenantAContext = TestDbContextFactory.CreateTenantContextMock(_tenantAId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantAContext.Object);

        // Act - Silmeye çalış
        var customerToDelete = await dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == customerBId);

        // Assert
        customerToDelete.Should().BeNull("Tenant A, Tenant B'nin müşterisini göremez");
    }

    #endregion
}
