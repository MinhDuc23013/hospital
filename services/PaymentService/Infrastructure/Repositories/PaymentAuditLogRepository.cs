using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure.Repositories;

public class PaymentAuditLogRepository : IPaymentAuditLogRepository
{
    private readonly PaymentDbContext _db;

    public PaymentAuditLogRepository(PaymentDbContext db) => _db = db;

    public async Task AddAsync(PaymentAuditLog log, CancellationToken ct = default)
        => await _db.PaymentAuditLogs.AddAsync(log, ct);

    public async Task<List<PaymentAuditLog>> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct = default)
        => await _db.PaymentAuditLogs
            .Where(l => l.PaymentId == paymentId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
