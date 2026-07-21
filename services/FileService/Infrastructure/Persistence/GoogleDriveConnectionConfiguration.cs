using FileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Persistence;

public class GoogleDriveConnectionConfiguration : IEntityTypeConfiguration<GoogleDriveConnection>
{
    public void Configure(EntityTypeBuilder<GoogleDriveConnection> builder)
    {
        builder.ToTable("google_drive_connections");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.AccessToken).IsRequired().HasMaxLength(2048);
        builder.Property(c => c.RefreshToken).IsRequired().HasMaxLength(2048);
        builder.Property(c => c.ExpiresAt).IsRequired();
        builder.Property(c => c.ConnectedEmail).IsRequired().HasMaxLength(255);
        builder.Property(c => c.ConnectedAt).IsRequired();
    }
}
