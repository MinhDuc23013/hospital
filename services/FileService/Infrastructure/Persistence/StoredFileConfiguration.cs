using FileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Persistence;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("stored_files");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.FileName).IsRequired().HasMaxLength(255);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(128);
        builder.Property(f => f.SizeBytes).IsRequired();
        builder.Property(f => f.Sha256).HasMaxLength(64).HasColumnType("char(64)");
        builder.Property(f => f.UploadedBy).HasMaxLength(100);
        builder.Property(f => f.UploadedAt).HasDefaultValueSql("NOW()");
        builder.Property(f => f.IsActive).HasDefaultValue(true);
        builder.HasIndex(f => f.UploadedBy);
        builder.HasIndex(f => f.UploadedAt);
    }
}
