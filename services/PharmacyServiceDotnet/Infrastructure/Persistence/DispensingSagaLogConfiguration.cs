using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Persistence;

public class DispensingSagaLogConfiguration : IEntityTypeConfiguration<DispensingSagaLog>
{
    public void Configure(EntityTypeBuilder<DispensingSagaLog> builder)
    {
        builder.ToTable("dispensing_saga_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.SagaId).IsRequired();
        builder.Property(l => l.FromStep).IsRequired().HasMaxLength(30);
        builder.Property(l => l.ToStep).IsRequired().HasMaxLength(30);
        builder.Property(l => l.Message).HasMaxLength(500);
        builder.Property(l => l.Details).HasColumnType("text");
        builder.Property(l => l.Timestamp).IsRequired();

        builder.HasIndex(l => l.SagaId);
    }
}
