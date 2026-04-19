using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Persistence;

public class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> builder)
    {
        builder.ToTable("cash_sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.CashierId).IsRequired().HasMaxLength(100);
        builder.Property(s => s.CashierName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.CounterId).IsRequired().HasMaxLength(50);
        builder.Property(s => s.OpeningBalance).HasPrecision(18, 2);
        builder.Property(s => s.ExpectedCash).HasPrecision(18, 2);
        builder.Property(s => s.ActualCash).HasPrecision(18, 2);
        builder.Property(s => s.Variance).HasPrecision(18, 2);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.OpenedAt).HasDefaultValueSql("NOW()");
        builder.HasIndex(s => new { s.CashierId, s.Status });
    }
}
