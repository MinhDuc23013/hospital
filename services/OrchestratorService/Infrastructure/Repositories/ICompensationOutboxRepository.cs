using OrchestratorService.Domain.Entities;

namespace OrchestratorService.Infrastructure.Repositories;

public interface ICompensationOutboxRepository
{
    Task AddAsync(CompensationOutbox item, CancellationToken ct = default);
    Task<List<CompensationOutbox>> GetPendingAsync(int batchSize, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
