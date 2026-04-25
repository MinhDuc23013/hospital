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
        builder.Property(s => s.PaymentMethod).IsRequired().HasMaxLength(50);
        builder.Property(s => s.PaymentAmount).HasPrecision(18, 2);
        builder.Property(s => s.ScheduledTime).IsRequired();
        builder.Property(s => s.DurationMinutes).IsRequired();
        builder.Property(s => s.CurrentStep).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.FailureReason).HasMaxLength(1000);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");
        builder.HasIndex(s => s.PatientId);
        builder.HasIndex(s => s.AppointmentId);
        builder.HasIndex(s => s.CurrentStep);
    }
}
