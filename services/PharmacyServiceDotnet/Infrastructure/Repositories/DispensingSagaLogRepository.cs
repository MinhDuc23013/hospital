using PharmacyServiceDotnet.Domain.Entities;
using PharmacyServiceDotnet.Infrastructure.Persistence;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public class DispensingSagaLogRepository : IDispensingSagaLogRepository
{
    private readonly PharmacyDbContext _context;
    public DispensingSagaLogRepository(PharmacyDbContext context) => _context = context;

    public Task AddAsync(DispensingSagaLog log, CancellationToken ct = default)
        => _context.DispensingSagaLogs.AddAsync(log, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
