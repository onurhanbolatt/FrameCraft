using FrameCraft.Domain.Entities.Authentication;
using FrameCraft.Domain.Entities.Core;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Entities.Inventory;
using FrameCraft.Domain.Entities.Sales;
using FrameCraft.Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;

namespace FrameCraft.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<UploadedFile> UploadedFiles { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Company> Companies { get; }
    DbSet<Frame> Frames { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleLine> SaleLines { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}