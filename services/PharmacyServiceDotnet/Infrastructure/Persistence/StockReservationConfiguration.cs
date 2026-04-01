using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Persistence;

public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.PrescriptionId).IsRequired();
        builder.Property(r => r.DrugBatchId).IsRequired();
        builder.Property(r => r.DrugId).IsRequired();
        builder.Property(r => r.Quantity).IsRequired();
        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();
        builder.Property(r => r.ExpiresAt).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        builder.HasIndex(r => r.PrescriptionId);
        builder.HasIndex(r => r.DrugBatchId);
        builder.HasIndex(r => new { r.Status, r.ExpiresAt }); // For expiry worker queries
    }
}
