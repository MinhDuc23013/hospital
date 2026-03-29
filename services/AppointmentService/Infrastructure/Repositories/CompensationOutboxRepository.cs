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

    /// <summary>Get pending items due for retry, ordered by next retry time.</summary>
    public Task<List<CompensationOutbox>> GetPendingAsync(int batchSize, CancellationToken ct = default)
        => _context.CompensationOutbox
            .Where(c => !c.IsCompleted && c.RetryCount < c.MaxRetries && c.NextRetryAt <= DateTime.Now)
            .OrderBy(c => c.NextRetryAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
