using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(10);
        builder.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.TransactionId).HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");
        // Cash-specific fields
        builder.Property(p => p.AmountReceived).HasPrecision(18, 2);
        builder.Property(p => p.ChangeReturned).HasPrecision(18, 2);
        builder.Property(p => p.CashierId).HasMaxLength(100);
        builder.Property(p => p.ReceiptNumber).HasMaxLength(50);
        builder.HasIndex(p => p.ReceiptNumber).IsUnique().HasFilter("\"ReceiptNumber\" IS NOT NULL");
        builder.HasIndex(p => p.CashSessionId);
        builder.HasIndex(p => p.AppointmentId);
        builder.HasIndex(p => p.PatientId);
        builder.HasIndex(p => new { p.Status, p.CreatedAt });
    }
}
