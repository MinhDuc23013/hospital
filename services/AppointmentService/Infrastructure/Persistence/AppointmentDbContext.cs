using AppointmentService.Domain.Entities;
using HospitalShared.Outbox;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Persistence;

public class AppointmentDbContext : DbContext
{
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<BookingSaga> BookingSagas => Set<BookingSaga>();
    public DbSet<BookingSagaLog> BookingSagaLogs => Set<BookingSagaLog>();
    public DbSet<CompensationOutbox> CompensationOutbox => Set<CompensationOutbox>();
    public DbSet<EventOutbox> EventOutbox => Set<EventOutbox>();

    public AppointmentDbContext(DbContextOptions<AppointmentDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AppointmentConfiguration());
        modelBuilder.ApplyConfiguration(new BookingSagaConfiguration());
        modelBuilder.ApplyConfiguration(new BookingSagaLogConfiguration());
        modelBuilder.ApplyConfiguration(new CompensationOutboxConfiguration());
        modelBuilder.ApplyConfiguration(new EventOutboxConfiguration());
    }
}
