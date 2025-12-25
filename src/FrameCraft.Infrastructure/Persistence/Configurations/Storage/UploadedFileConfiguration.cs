using FrameCraft.Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrameCraft.Infrastructure.Persistence.Configurations.Storage;

public class UploadedFileConfiguration : IEntityTypeConfiguration<UploadedFile>
{
    public void Configure(EntityTypeBuilder<UploadedFile> builder)
    {
        builder.ToTable("UploadedFiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.FileSize)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Folder)
            .HasMaxLength(200);

        builder.Property(x => x.EntityType)
            .HasMaxLength(100);

        builder.Property(x => x.Category)
            .HasMaxLength(100);

        builder.Property(x => x.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(x => x.IsPublic)
            .HasDefaultValue(false);

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(x => x.FileKey)
            .IsUnique();

        builder.HasIndex(x => new { x.EntityId, x.EntityType });
        
        builder.HasIndex(x => x.Category);
        
        builder.HasIndex(x => x.Folder);

        builder.HasIndex(x => x.TenantId);

        builder.HasIndex(x => x.UploadedBy);

        builder.HasIndex(x => x.CreatedAt);
    }
}
