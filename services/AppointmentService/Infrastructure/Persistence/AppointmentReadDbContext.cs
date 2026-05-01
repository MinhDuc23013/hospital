using AppointmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Persistence;

// Read-only DbContext pointing to the read replica.
// No change tracking, no outbox — queries only.
public class AppointmentReadDbContext : DbContext
{
    public DbSet<Appointment> Appointments => Set<Appointment>();

    public AppointmentReadDbContext(DbContextOptions<AppointmentReadDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfiguration(new AppointmentConfiguration());

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
