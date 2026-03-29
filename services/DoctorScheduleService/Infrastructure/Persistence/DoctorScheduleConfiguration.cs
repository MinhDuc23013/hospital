using DoctorScheduleService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoctorScheduleService.Infrastructure.Persistence;

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("doctor_schedules");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.DoctorId).IsRequired().HasMaxLength(100);
        builder.Property(s => s.DoctorName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");
        builder.HasIndex(s => s.DoctorId);
        builder.HasIndex(s => new { s.DoctorId, s.Date });

        builder.HasMany(s => s.Slots)
            .WithOne()
            .HasForeignKey(sl => sl.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Slots).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
