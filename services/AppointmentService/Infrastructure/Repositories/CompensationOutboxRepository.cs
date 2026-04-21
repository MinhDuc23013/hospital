using AppointmentService.Domain.Entities;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class CompensationOutboxRepository : ICompensationOutboxRepository
{
    private readonly AppointmentDbContext _context;
    public CompensationOutboxRepository(AppointmentDbContext context) => _context = context;

    public Task AddAsync(CompensationOutbox item, CancellationToken ct = default)
        => _context.CompensationOutbox.AddAsync(item, ct).AsTask();

    /// <summary>
    /// Gets pending items due for retry, using FOR UPDATE SKIP LOCKED so two replicas
    /// split the batch and never double-invoke external compensation (refund, release).
    /// MUST be called inside an open DB transaction.
    /// </summary>
    public Task<List<CompensationOutbox>> GetPendingAsync(int batchSize, CancellationToken ct = default)
        => _context.CompensationOutbox
            .FromSqlRaw(
                @"SELECT * FROM compensation_outbox
                  WHERE ""IsCompleted"" = false
                    AND ""RetryCount"" < ""MaxRetries""
                    AND ""NextRetryAt"" <= {0}
                  ORDER BY ""NextRetryAt""
                  LIMIT {1}
                  FOR UPDATE SKIP LOCKED",
                DateTime.Now, batchSize)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
