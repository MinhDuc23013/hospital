using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrchestratorService.Domain.Entities;

namespace OrchestratorService.Infrastructure.Persistence;

public class BookingSagaConfiguration : IEntityTypeConfiguration<BookingSaga>
{
    public void Configure(EntityTypeBuilder<BookingSaga> builder)
    {
        builder.ToTable("booking_sagas");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.DoctorId).IsRequired().HasMaxLength(100);
        builder.Property(s => s.ScheduledTime).IsRequired();
        builder.Property(s => s.DurationMinutes).IsRequired();
        builder.Property(s => s.CurrentStep).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.FailureReason).HasMaxLength(1000);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(s => s.Version).IsConcurrencyToken().HasDefaultValue(0u);
        builder.HasIndex(s => s.PatientId);
        builder.HasIndex(s => s.AppointmentId);
        builder.HasIndex(s => s.CurrentStep);
        // Prevent duplicate active bookings for the same patient+doctor+slot at DB level.
        // Partial index excludes terminal states so completed/failed sagas don't block re-booking.
        builder.HasIndex(s => new { s.PatientId, s.DoctorId, s.SlotId })
            .IsUnique()
            .HasFilter("\"CurrentStep\" NOT IN ('Failed','Compensated','Completed')")
            .HasDatabaseName("ux_booking_sagas_active_slot");
    }
}
