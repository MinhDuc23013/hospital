using HospitalShared.Outbox;
using Microsoft.EntityFrameworkCore;
using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Persistence;

public class PharmacyDbContext : DbContext
{
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<DrugBatch> DrugBatches => Set<DrugBatch>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<InventoryAuditLog> InventoryAuditLogs => Set<InventoryAuditLog>();
    public DbSet<DispensingSaga> DispensingSagas => Set<DispensingSaga>();
    public DbSet<DispensingSagaLog> DispensingSagaLogs => Set<DispensingSagaLog>();
    public DbSet<EventOutbox> EventOutbox => Set<EventOutbox>();

    public PharmacyDbContext(DbContextOptions<PharmacyDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DrugConfiguration());
        modelBuilder.ApplyConfiguration(new PrescriptionConfiguration());
        modelBuilder.ApplyConfiguration(new DrugBatchConfiguration());
        modelBuilder.ApplyConfiguration(new StockReservationConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new DispensingSagaConfiguration());
        modelBuilder.ApplyConfiguration(new DispensingSagaLogConfiguration());
        modelBuilder.ApplyConfiguration(new EventOutboxConfiguration());
    }
}
