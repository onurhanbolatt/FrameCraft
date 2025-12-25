using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Entities.Core;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Entities.Storage;
using FrameCraft.Domain.Enums;

namespace FrameCraft.UnitTests.Common;

public static class TestDataBuilder
{
    #region Tenant Builder

    public static Tenant CreateTenant(
        string name = "Test Tenant",
        string subdomain = "test",
        TenantStatus status = TenantStatus.Active)
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Subdomain = subdomain,
            Status = status,
            MaxUsers = 10,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    #endregion

    #region User Builder

    public static User CreateUser(
        Guid tenantId,
        string email = "test@test.com",
        string firstName = "Test",
        string lastName = "User",
        bool isSuperAdmin = false,
        bool isActive = true)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            FirstName = firstName,
            LastName = lastName,
            IsSuperAdmin = isSuperAdmin,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public static User CreateSuperAdmin(
        string email = "admin@system.com",
        string firstName = "Super",
        string lastName = "Admin")
    {
        return new User
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            FirstName = firstName,
            LastName = lastName,
            IsSuperAdmin = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    #endregion

    #region Role Builder

    public static Role CreateRole(string name = "User", string description = "Standard User")
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public static List<Role> CreateDefaultRoles()
    {
        return new List<Role>
        {
            new Role
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Admin",
                Description = "Yönetici - Tüm yetkilere sahip",
                CreatedAt = DateTime.UtcNow
            },
            new Role
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "User",
                Description = "Standart Kullanıcı",
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    #endregion

    #region Customer Builder

    public static Customer CreateCustomer(
        Guid tenantId,
        string name = "Test Customer",
        string? email = "customer@test.com",
        bool isActive = true)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Email = email,
            Phone = "555-1234",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public static List<Customer> CreateCustomers(Guid tenantId, int count = 5)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateCustomer(tenantId, $"Customer {i}", $"customer{i}@test.com"))
            .ToList();
    }

    #endregion

    #region UploadedFile Builder

    public static UploadedFile CreateUploadedFile(
        Guid tenantId,
        string fileName = "test.pdf",
        string fileKey = "tenant-xxx/2024/01/test.pdf")
    {
        return UploadedFile.Create(
            fileKey: fileKey,
            fileName: fileName,
            originalFileName: fileName,
            contentType: "application/pdf",
            fileSize: 1024,
            uploadedBy: Guid.NewGuid(),
            folder: "documents",
            description: "Test file");
    }

    #endregion

    #region RefreshToken Builder

    public static RefreshToken CreateRefreshToken(
        Guid userId,
        bool isExpired = false,
        bool isRevoked = false)
    {
        var expiresAt = isExpired ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(7);

        return new RefreshToken
        {
            UserId = userId,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = expiresAt,
            CreatedByIp = "127.0.0.1",
            IsRevoked = isRevoked,
            RevokedAt = isRevoked ? DateTime.UtcNow : null,
            RevokedByIp = isRevoked ? "127.0.0.1" : null,
            ReplacedByToken = null
        };
    }

    #endregion
}
