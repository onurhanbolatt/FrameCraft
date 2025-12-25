using FrameCraft.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrameCraft.Infrastructure.Persistence.Configurations.Sales;

public class SaleLineConfiguration : IEntityTypeConfiguration<SaleLine>
{
    public void Configure(EntityTypeBuilder<SaleLine> builder)
    {
        builder.ToTable("SaleLines");

        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.LineType)
            .IsRequired();

        builder.Property(sl => sl.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(sl => sl.LineTotal)
            .HasPrecision(18, 2);

        builder.Property(sl => sl.LineDiscount)
            .HasPrecision(18, 2);

        builder.Property(sl => sl.LineFinalAmount)
            .HasPrecision(18, 2);

        builder.Property(sl => sl.Notes)
            .HasMaxLength(500);

        // Index'ler
        builder.HasIndex(sl => sl.TenantId);
        builder.HasIndex(sl => sl.SaleId);
        builder.HasIndex(sl => sl.FrameId);

        // İlişkiler
        builder.HasOne(sl => sl.Sale)
            .WithMany(s => s.Lines)
            .HasForeignKey(sl => sl.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sl => sl.Frame)
            .WithMany(f => f.SaleLines)
            .HasForeignKey(sl => sl.FrameId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(sl => !sl.IsDeleted);
    }
}
