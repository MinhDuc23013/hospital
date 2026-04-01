using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public interface IDispensingSagaRepository
{
    Task<DispensingSaga?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DispensingSaga?> GetByPrescriptionIdAsync(Guid prescriptionId, CancellationToken ct = default);
    Task AddAsync(DispensingSaga saga, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
