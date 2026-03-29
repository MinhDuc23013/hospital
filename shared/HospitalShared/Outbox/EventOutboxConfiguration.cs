using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalShared.Outbox;

public class EventOutboxConfiguration : IEntityTypeConfiguration<EventOutbox>
{
    public void Configure(EntityTypeBuilder<EventOutbox> builder)
    {
        builder.ToTable("event_outbox");
        builder.Property(e => e.Topic).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Key).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Value).IsRequired();
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastError).HasMaxLength(500);
        builder.HasIndex(e => new { e.IsSent, e.CreatedAt }).HasFilter("NOT \"IsSent\"");
    }
}
