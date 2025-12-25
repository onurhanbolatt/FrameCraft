using FrameCraft.Domain.Entities.CRM;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrameCraft.Infrastructure.Persistence.Configurations.Core.CRM;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Phone)
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(100);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.Property(c => c.Notes)
            .HasMaxLength(1000);

        // Index'ler
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.TenantId, c.Code }).IsUnique(); // Tenant içinde kod benzersiz

        // İlişkiler
        builder.HasMany(c => c.Frames)
            .WithOne(f => f.Company)
            .HasForeignKey(f => f.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
