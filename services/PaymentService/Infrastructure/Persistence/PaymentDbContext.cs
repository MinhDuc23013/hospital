using HospitalShared.Outbox;
using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAuditLog> PaymentAuditLogs => Set<PaymentAuditLog>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<EventOutbox> EventOutbox => Set<EventOutbox>();

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new CashSessionConfiguration());
        modelBuilder.ApplyConfiguration(new EventOutboxConfiguration());

        // PostgreSQL sequence for receipt numbers (sequential, no gaps)
        modelBuilder.HasSequence<long>("receipt_seq").StartsAt(1).IncrementsBy(1);
    }
}
