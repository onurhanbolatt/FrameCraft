using FrameCraft.Domain.Entities.CRM;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrameCraft.Infrastructure.Persistence.Configurations.CRM;

/// <summary>
/// Customer tablosu yapılandırması
/// Hangi kolon ne kadar uzun, hangi alan zorunlu, index'ler vb.
/// </summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        // Tablo adı
        builder.ToTable("Customers");

        // Primary Key
        builder.HasKey(c => c.Id);

        // Properties (Kolonlar)
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Phone)
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(100);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.Property(c => c.Notes)
            .HasMaxLength(1000);

        // Index'ler (Arama performansı için)
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => c.Phone);
        builder.HasIndex(c => new { c.TenantId, c.IsActive });

        // İlişkiler (Relationships)
        builder.HasMany(c => c.Sales)
            .WithOne(s => s.Customer)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict); // Müşteri silinemez eğer satışı varsa

        // Soft Delete için global filter
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
