using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public interface IInventoryAuditLogRepository
{
    Task AddAsync(InventoryAuditLog log, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
