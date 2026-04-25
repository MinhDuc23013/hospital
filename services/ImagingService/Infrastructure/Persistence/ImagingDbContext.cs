using HospitalShared.Outbox;
using ImagingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImagingService.Infrastructure.Persistence;

public class ImagingDbContext : DbContext
{
    public DbSet<ImagingOrder> ImagingOrders => Set<ImagingOrder>();
    public DbSet<ImagingResult> ImagingResults => Set<ImagingResult>();
    public DbSet<EventOutbox> EventOutbox => Set<EventOutbox>();

    public ImagingDbContext(DbContextOptions<ImagingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ImagingOrderConfiguration());
        modelBuilder.ApplyConfiguration(new ImagingResultConfiguration());
        modelBuilder.ApplyConfiguration(new EventOutboxConfiguration());
    }
}
