using Microsoft.EntityFrameworkCore;
using PharmacyServiceDotnet.Domain.Entities;
using PharmacyServiceDotnet.Infrastructure.Persistence;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public class DispensingSagaRepository : IDispensingSagaRepository
{
    private readonly PharmacyDbContext _context;
    public DispensingSagaRepository(PharmacyDbContext context) => _context = context;

    public Task<DispensingSaga?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.DispensingSagas.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<DispensingSaga?> GetByPrescriptionIdAsync(Guid prescriptionId, CancellationToken ct = default)
        => _context.DispensingSagas.FirstOrDefaultAsync(s => s.PrescriptionId == prescriptionId, ct);

    public Task AddAsync(DispensingSaga saga, CancellationToken ct = default)
        => _context.DispensingSagas.AddAsync(saga, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
