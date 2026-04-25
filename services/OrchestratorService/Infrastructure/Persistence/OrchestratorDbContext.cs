using HospitalShared.Outbox;
using Microsoft.EntityFrameworkCore;
using OrchestratorService.Domain.Entities;

namespace OrchestratorService.Infrastructure.Persistence;

/// <summary>
/// DbContext for OrchestratorService — owns only saga/outbox tables.
/// NO Appointment entity — appointments are managed exclusively by AppointmentService.
/// </summary>
public class OrchestratorDbContext : DbContext
{
    public DbSet<BookingSaga> BookingSagas => Set<BookingSaga>();
    public DbSet<BookingSagaLog> BookingSagaLogs => Set<BookingSagaLog>();
    public DbSet<PaymentSaga> PaymentSagas => Set<PaymentSaga>();
    public DbSet<CompensationOutbox> CompensationOutbox => Set<CompensationOutbox>();
    public DbSet<EventOutbox> EventOutbox => Set<EventOutbox>();

    public OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BookingSagaConfiguration());
        modelBuilder.ApplyConfiguration(new BookingSagaLogConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentSagaConfiguration());
        modelBuilder.ApplyConfiguration(new CompensationOutboxConfiguration());
        modelBuilder.ApplyConfiguration(new EventOutboxConfiguration());
    }
}
