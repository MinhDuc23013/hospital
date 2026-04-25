using HospitalShared.Outbox;
using LabTestService.Domain.Entities;
using LabTestService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabTestService.Infrastructure.Persistence;

public class LabTestDbContext : DbContext
{
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<LabOrderItem> LabOrderItems => Set<LabOrderItem>();
    public DbSet<EventOutbox> EventOutbox => Set<EventOutbox>();

    public LabTestDbContext(DbContextOptions<LabTestDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new LabOrderConfiguration());
        modelBuilder.ApplyConfiguration(new EventOutboxConfiguration());
    }
}
