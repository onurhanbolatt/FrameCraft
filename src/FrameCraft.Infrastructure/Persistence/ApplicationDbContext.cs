using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Domain.Entities.Core;
using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Entities.Common;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Entities.Inventory;
using FrameCraft.Domain.Entities.Sales;
using FrameCraft.Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;

namespace FrameCraft.Infrastructure.Persistence;

/// <summary>
/// Ana veritabanı context - Entity Framework Core
/// Multi-tenancy ve soft delete için global query filters içerir
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext? _tenantContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Core DbSets
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantFeature> TenantFeatures { get; set; }

    // Authentication DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    // CRM DbSets
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Company> Companies { get; set; }

    // Inventory DbSets
    public DbSet<Frame> Frames { get; set; }

    // Sales DbSets
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleLine> SaleLines { get; set; }

    // Storage DbSets
    public DbSet<UploadedFile> UploadedFiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global Query Filters for TenantEntity
        ConfigureTenantFilters(modelBuilder);
    }

    private void ConfigureTenantFilters(ModelBuilder modelBuilder)
    {
        // Customer - TenantEntity
        modelBuilder.Entity<Customer>().HasQueryFilter(e => 
            !e.IsDeleted && 
            (!IsTenantFilteringEnabled || e.TenantId == CurrentTenantId));

        // Company - TenantEntity
        modelBuilder.Entity<Company>().HasQueryFilter(e => 
            !e.IsDeleted && 
            (!IsTenantFilteringEnabled || e.TenantId == CurrentTenantId));

        // Frame - TenantEntity
        modelBuilder.Entity<Frame>().HasQueryFilter(e => 
            !e.IsDeleted && 
            (!IsTenantFilteringEnabled || e.TenantId == CurrentTenantId));

        // Sale - TenantEntity
        modelBuilder.Entity<Sale>().HasQueryFilter(e => 
            !e.IsDeleted && 
            (!IsTenantFilteringEnabled || e.TenantId == CurrentTenantId));

        // SaleLine - TenantEntity
        modelBuilder.Entity<SaleLine>().HasQueryFilter(e => 
            !e.IsDeleted && 
            (!IsTenantFilteringEnabled || e.TenantId == CurrentTenantId));

        // UploadedFile - TenantEntity
        modelBuilder.Entity<UploadedFile>().HasQueryFilter(e => 
            !e.IsDeleted && 
            (!IsTenantFilteringEnabled || e.TenantId == CurrentTenantId));

        // User - TenantId'ye göre filtrele (ama SuperAdmin hepsini görebilir)
        modelBuilder.Entity<User>().HasQueryFilter(e => 
            !e.IsDeleted && 
            (!IsTenantFilteringEnabled || e.TenantId == CurrentTenantId));

        // RefreshToken - Soft delete only
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => !e.IsDeleted);

        // Role - Soft delete only (roller tenant'a bağlı değil)
        modelBuilder.Entity<Role>().HasQueryFilter(e => !e.IsDeleted);

        // Tenant - Soft delete only
        modelBuilder.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
    }

    /// <summary>
    /// Mevcut tenant ID (query filter için)
    /// </summary>
    private Guid CurrentTenantId => _tenantContext?.CurrentTenantId ?? Guid.Empty;

    /// <summary>
    /// Tenant filtreleme aktif mi?
    /// </summary>
    private bool IsTenantFilteringEnabled => _tenantContext?.IsTenantFilteringEnabled ?? false;

    /// <summary>
    /// SaveChanges'ta otomatik TenantId ve audit field ataması
    /// </summary>
    public override int SaveChanges()
    {
        SetTenantIdForNewEntities();
        return base.SaveChanges();
    }

    /// <summary>
    /// SaveChangesAsync'te otomatik TenantId ve audit field ataması
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTenantIdForNewEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Yeni eklenen TenantEntity'lere otomatik TenantId ata
    /// </summary>
    private void SetTenantIdForNewEntities()
    {
        var tenantId = _tenantContext?.CurrentTenantId;

        var entries = ChangeTracker.Entries<TenantEntity>()
            .Where(e => e.State == EntityState.Added && e.Entity.TenantId == Guid.Empty);

        foreach (var entry in entries)
        {
            // ✅ SuperAdmin user için tenant zorunluluğu yok
            if (entry.Entity is FrameCraft.Domain.Entities.Authentication.User u && u.IsSuperAdmin)
            {
                continue;
            }

            // ❌ Diğer tüm tenant-scoped entity’ler için zorunlu
            if (!tenantId.HasValue || tenantId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"TenantId is required for entity '{entry.Entity.GetType().Name}'. " +
                    "SuperAdmin must select a tenant before creating tenant-specific data.");
            }

            entry.Entity.TenantId = tenantId.Value;
        }
    }

}
