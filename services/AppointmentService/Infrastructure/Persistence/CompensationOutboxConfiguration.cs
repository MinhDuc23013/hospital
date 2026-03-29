using AppointmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentService.Infrastructure.Persistence;

public class CompensationOutboxConfiguration : IEntityTypeConfiguration<CompensationOutbox>
{
    public void Configure(EntityTypeBuilder<CompensationOutbox> builder)
    {
        builder.ToTable("compensation_outbox");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ActionType).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Payload).IsRequired().HasMaxLength(2000);
        builder.Property(c => c.LastError).HasMaxLength(1000);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");
        builder.HasIndex(c => c.SagaId);
        builder.HasIndex(c => new { c.IsCompleted, c.NextRetryAt });
    }
}
