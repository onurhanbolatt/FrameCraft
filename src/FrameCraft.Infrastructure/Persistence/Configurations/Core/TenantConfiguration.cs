using FrameCraft.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrameCraft.Infrastructure.Persistence.Configurations.Core;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", "dbo"); // Master schema'da

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Subdomain)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Phone)
            .HasMaxLength(20);

        builder.Property(t => t.Email)
            .HasMaxLength(100);

        // Index'ler
        builder.HasIndex(t => t.Subdomain).IsUnique();

        // İlişkiler
        builder.HasMany(t => t.Features)
            .WithOne(tf => tf.Tenant)
            .HasForeignKey(tf => tf.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Users) 
            .WithOne(u => u.Tenant)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}

public class TenantFeatureConfiguration : IEntityTypeConfiguration<TenantFeature>
{
    public void Configure(EntityTypeBuilder<TenantFeature> builder)
    {
        builder.ToTable("TenantFeatures", "dbo");

        builder.HasKey(tf => tf.Id);

        builder.Property(tf => tf.FeatureName)
            .IsRequired()
            .HasMaxLength(100);

        // Index'ler
        builder.HasIndex(tf => tf.TenantId);
        builder.HasIndex(tf => new { tf.TenantId, tf.FeatureName }).IsUnique();

        // İlişkiler
        builder.HasOne(tf => tf.Tenant)
            .WithMany(t => t.Features)
            .HasForeignKey(tf => tf.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(tf => !tf.IsDeleted);
    }
}
