using DoctorScheduleService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DoctorScheduleService.Infrastructure.Persistence;

public class DoctorScheduleDbContext : DbContext
{
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSchedule> Schedules => Set<DoctorSchedule>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();

    public DoctorScheduleDbContext(DbContextOptions<DoctorScheduleDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DoctorConfiguration());
        modelBuilder.ApplyConfiguration(new DoctorScheduleConfiguration());
        modelBuilder.ApplyConfiguration(new TimeSlotConfiguration());
    }
}
