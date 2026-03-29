using HospitalShared.Outbox;
using Microsoft.EntityFrameworkCore;
using PatientService.Domain.Entities;

namespace PatientService.Infrastructure.Persistence;

public class PatientDbContext : DbContext
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<EventOutbox> EventOutbox => Set<EventOutbox>();

    public PatientDbContext(DbContextOptions<PatientDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PatientConfiguration());
        modelBuilder.ApplyConfiguration(new EventOutboxConfiguration());
    }
}
