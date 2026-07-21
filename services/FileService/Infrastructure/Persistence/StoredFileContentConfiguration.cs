using FileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Persistence;

public class StoredFileContentConfiguration : IEntityTypeConfiguration<StoredFileContent>
{
    public void Configure(EntityTypeBuilder<StoredFileContent> builder)
    {
        builder.ToTable("stored_file_contents");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Content).IsRequired().HasColumnType("bytea");
    }
}
