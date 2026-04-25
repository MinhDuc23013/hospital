using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrchestratorService.Domain.Entities;

namespace OrchestratorService.Infrastructure.Persistence;

public class BookingSagaLogConfiguration : IEntityTypeConfiguration<BookingSagaLog>
{
    public void Configure(EntityTypeBuilder<BookingSagaLog> builder)
    {
        builder.ToTable("booking_saga_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.FromStep).IsRequired().HasMaxLength(30);
        builder.Property(l => l.ToStep).IsRequired().HasMaxLength(30);
        builder.Property(l => l.Message).HasMaxLength(500);
        builder.Property(l => l.Details).HasMaxLength(4000);
        builder.HasIndex(l => l.SagaId);
        builder.HasIndex(l => l.Timestamp);
    }
}
