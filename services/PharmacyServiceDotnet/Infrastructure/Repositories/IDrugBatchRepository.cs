using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public interface IDrugBatchRepository
{
    Task<DrugBatch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<DrugBatch>> GetByDrugIdAsync(Guid drugId, CancellationToken ct = default);

    /// <summary>Gets non-expired batches with available stock, ordered by ExpiryDate ASC (FEFO).</summary>
    Task<List<DrugBatch>> GetAvailableBatchesFEFOAsync(Guid drugId, CancellationToken ct = default);

    Task AddAsync(DrugBatch batch, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
