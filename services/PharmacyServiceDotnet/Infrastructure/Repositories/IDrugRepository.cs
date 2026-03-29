using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public interface IDrugRepository
{
    Task<Drug?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Drug?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<(List<Drug> Items, int Total)> ListAsync(string? name, bool? lowStock, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Drug drug, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
