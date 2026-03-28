using AppointmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointmentService.Infrastructure.Persistence;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ProviderId).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Notes).HasMaxLength(2000);
        builder.HasIndex(a => a.PatientId);
        builder.HasIndex(a => new { a.ProviderId, a.ScheduledTime });
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("NOW()");
    }
}
