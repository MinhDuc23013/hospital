using Microsoft.EntityFrameworkCore;
using PharmacyServiceDotnet.Domain.Entities;
using PharmacyServiceDotnet.Infrastructure.Persistence;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public class DrugBatchRepository : IDrugBatchRepository
{
    private readonly PharmacyDbContext _context;
    public DrugBatchRepository(PharmacyDbContext context) => _context = context;

    public Task<DrugBatch?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.DrugBatches.FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<List<DrugBatch>> GetByDrugIdAsync(Guid drugId, CancellationToken ct = default)
        => _context.DrugBatches
            .Where(b => b.DrugId == drugId)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(ct);

    /// <summary>FEFO: non-expired batches with available stock, earliest expiry first.</summary>
    public Task<List<DrugBatch>> GetAvailableBatchesFEFOAsync(Guid drugId, CancellationToken ct = default)
        => _context.DrugBatches
            .Where(b => b.DrugId == drugId
                && b.ExpiryDate > DateTime.Now
                && (b.Quantity - b.ReservedQuantity) > 0)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(ct);

    public Task AddAsync(DrugBatch batch, CancellationToken ct = default)
        => _context.DrugBatches.AddAsync(batch, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
