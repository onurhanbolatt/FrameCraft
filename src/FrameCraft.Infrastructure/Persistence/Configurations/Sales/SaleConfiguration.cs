using FrameCraft.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrameCraft.Infrastructure.Persistence.Configurations.Sales;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Status)
            .IsRequired();

        builder.Property(s => s.GrossAmount)
            .HasPrecision(18, 2);

        builder.Property(s => s.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(s => s.NetAmount)
            .HasPrecision(18, 2);

        builder.Property(s => s.Notes)
            .HasMaxLength(1000);

        // Index'ler
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => s.OrderNumber).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.Status });
        builder.HasIndex(s => s.CreatedAt);

        // İlişkiler
        builder.HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lines)
            .WithOne(sl => sl.Sale)
            .HasForeignKey(sl => sl.SaleId)
            .OnDelete(DeleteBehavior.Cascade); // Satış silinirse satırlar da silinir

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
