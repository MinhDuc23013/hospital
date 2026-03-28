using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentAuditLogConfiguration : IEntityTypeConfiguration<PaymentAuditLog>
{
    public void Configure(EntityTypeBuilder<PaymentAuditLog> builder)
    {
        builder.ToTable("payment_audit_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Action).IsRequired().HasMaxLength(50);
        builder.Property(l => l.OldStatus).HasMaxLength(20);
        builder.Property(l => l.NewStatus).IsRequired().HasMaxLength(20);
        builder.Property(l => l.Message).HasMaxLength(500);
        builder.Property(l => l.Details).HasMaxLength(4000);
        builder.HasIndex(l => l.PaymentId);
        builder.HasIndex(l => l.Timestamp);
    }
}
