using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public interface IDispensingSagaLogRepository
{
    Task AddAsync(DispensingSagaLog log, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
