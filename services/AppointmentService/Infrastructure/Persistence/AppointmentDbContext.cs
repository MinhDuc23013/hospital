using AppointmentService.Domain.Entities;
using HospitalShared.Outbox;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Persistence;

public class AppointmentDbContext : DbContext
{
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<EventOutbox> EventOutbox => Set<EventOutbox>();

    public AppointmentDbContext(DbContextOptions<AppointmentDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AppointmentConfiguration());
        modelBuilder.ApplyConfiguration(new EventOutboxConfiguration());
    }
}
