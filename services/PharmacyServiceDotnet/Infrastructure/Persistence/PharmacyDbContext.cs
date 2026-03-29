using HospitalShared.Outbox;
using Microsoft.EntityFrameworkCore;
using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Persistence;

public class PharmacyDbContext : DbContext
{
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<EventOutbox> EventOutbox => Set<EventOutbox>();

    public PharmacyDbContext(DbContextOptions<PharmacyDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DrugConfiguration());
        modelBuilder.ApplyConfiguration(new PrescriptionConfiguration());
        modelBuilder.ApplyConfiguration(new EventOutboxConfiguration());
    }
}
