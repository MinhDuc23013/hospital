using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Persistence;

public class DrugConfiguration : IEntityTypeConfiguration<Drug>
{
    public void Configure(EntityTypeBuilder<Drug> builder)
    {
        builder.ToTable("drugs");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Code).IsRequired().HasMaxLength(50);
        builder.Property(d => d.Dosage).HasMaxLength(100);
        builder.Property(d => d.Quantity).IsRequired().HasDefaultValue(0);
        builder.Property(d => d.Price).IsRequired().HasColumnType("numeric").HasDefaultValue(0);
        builder.Property(d => d.LowStockThreshold).IsRequired().HasDefaultValue(10);
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.UpdatedAt).IsRequired();
        builder.HasIndex(d => d.Code).IsUnique();
        builder.HasIndex(d => d.Name);
    }
}
