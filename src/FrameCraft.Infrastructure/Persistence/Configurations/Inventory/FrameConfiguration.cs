using FrameCraft.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrameCraft.Infrastructure.Persistence.Configurations.Inventory;

public class FrameConfiguration : IEntityTypeConfiguration<Frame>
{
    public void Configure(EntityTypeBuilder<Frame> builder)
    {
        builder.ToTable("Frames");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.Description)
            .HasMaxLength(500);

        builder.Property(f => f.Thickness)
            .HasPrecision(10, 2); // 10 digit, 2 ondalık

        builder.Property(f => f.CostPerMeter)
            .HasPrecision(18, 2);

        builder.Property(f => f.ProfitMargin)
            .HasPrecision(10, 2);

        builder.Property(f => f.PricePerMeter)
            .HasPrecision(18, 2);

        builder.Property(f => f.ImageUrl)
            .HasMaxLength(500);

        // Index'ler
        builder.HasIndex(f => f.TenantId);
        builder.HasIndex(f => f.CompanyId);
        builder.HasIndex(f => new { f.TenantId, f.CompanyId, f.Code }).IsUnique();

        // İlişkiler
        builder.HasOne(f => f.Company)
            .WithMany(c => c.Frames)
            .HasForeignKey(f => f.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.SaleLines)
            .WithOne(sl => sl.Frame)
            .HasForeignKey(sl => sl.FrameId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(f => !f.IsDeleted);
    }
}
