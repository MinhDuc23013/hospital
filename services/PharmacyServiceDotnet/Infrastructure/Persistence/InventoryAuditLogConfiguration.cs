using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Persistence;

public class InventoryAuditLogConfiguration : IEntityTypeConfiguration<InventoryAuditLog>
{
    public void Configure(EntityTypeBuilder<InventoryAuditLog> builder)
    {
        builder.ToTable("inventory_audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();
        builder.Property(a => a.DrugId).IsRequired();
        builder.Property(a => a.BatchNumber).HasMaxLength(100);
        builder.Property(a => a.UserId).HasMaxLength(100);
        builder.Property(a => a.Quantity).IsRequired();
        builder.Property(a => a.Details).HasColumnType("text");
        builder.Property(a => a.Timestamp).IsRequired();

        builder.HasIndex(a => a.DrugId);
        builder.HasIndex(a => a.PrescriptionId);
        builder.HasIndex(a => a.Timestamp);
    }
}
