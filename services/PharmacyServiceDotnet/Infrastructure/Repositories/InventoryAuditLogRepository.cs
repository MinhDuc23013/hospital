using PharmacyServiceDotnet.Domain.Entities;
using PharmacyServiceDotnet.Infrastructure.Persistence;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public class InventoryAuditLogRepository : IInventoryAuditLogRepository
{
    private readonly PharmacyDbContext _context;
    public InventoryAuditLogRepository(PharmacyDbContext context) => _context = context;

    public Task AddAsync(InventoryAuditLog log, CancellationToken ct = default)
        => _context.InventoryAuditLogs.AddAsync(log, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
