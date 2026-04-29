using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrchestratorService.Domain.Entities;

namespace OrchestratorService.Infrastructure.Persistence;

public class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("processed_events");
        builder.HasKey(e => new { e.EventId, e.ConsumerGroup });
        builder.Property(e => e.ConsumerGroup).HasMaxLength(100);
        builder.Property(e => e.ProcessedAt).IsRequired();
    }
}
