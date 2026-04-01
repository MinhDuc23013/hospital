using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Persistence;

public class DrugBatchConfiguration : IEntityTypeConfiguration<DrugBatch>
{
    public void Configure(EntityTypeBuilder<DrugBatch> builder)
    {
        builder.ToTable("drug_batches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.DrugId).IsRequired();
        builder.Property(b => b.BatchNumber).IsRequired().HasMaxLength(100);
        builder.Property(b => b.ExpiryDate).IsRequired();
        builder.Property(b => b.Quantity).IsRequired().HasDefaultValue(0);
        builder.Property(b => b.ReservedQuantity).IsRequired().HasDefaultValue(0);
        builder.Property(b => b.ReceivedDate).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();

        // Optimistic concurrency via EF Core RowVersion
        builder.Property(b => b.RowVersion).IsRowVersion();

        builder.HasIndex(b => b.DrugId);
        builder.HasIndex(b => new { b.DrugId, b.BatchNumber }).IsUnique();
        builder.HasIndex(b => b.ExpiryDate); // For FEFO queries
    }
}
