using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FrameCraft.UnitTests.Common;

/// <summary>
/// Test için InMemory DbContext factory
/// Multi-tenant testleri için tenant context mock'u ile birlikte kullanılır
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Yeni bir InMemory DbContext oluşturur
    /// </summary>
    public static ApplicationDbContext Create(ITenantContext? tenantContext = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options, tenantContext);
        context.Database.EnsureCreated();
        
        return context;
    }

    /// <summary>
    /// Belirli bir tenant için mock TenantContext oluşturur
    /// </summary>
    public static Mock<ITenantContext> CreateTenantContextMock(Guid tenantId, bool filteringEnabled = true)
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(x => x.CurrentTenantId).Returns(tenantId);
        mock.Setup(x => x.IsTenantFilteringEnabled).Returns(filteringEnabled);
        return mock;
    }

    /// <summary>
    /// SuperAdmin için mock TenantContext oluşturur (filtering kapalı)
    /// </summary>
    public static Mock<ITenantContext> CreateSuperAdminContextMock()
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(x => x.CurrentTenantId).Returns((Guid?)null);
        mock.Setup(x => x.IsTenantFilteringEnabled).Returns(false);
        return mock;
    }

    /// <summary>
    /// Tenant context yok (unauthorized) senaryosu
    /// </summary>
    public static Mock<ITenantContext> CreateNoTenantContextMock()
    {
        var mock = new Mock<ITenantContext>();
        mock.Setup(x => x.CurrentTenantId).Returns((Guid?)null);
        mock.Setup(x => x.IsTenantFilteringEnabled).Returns(true);
        return mock;
    }
}
