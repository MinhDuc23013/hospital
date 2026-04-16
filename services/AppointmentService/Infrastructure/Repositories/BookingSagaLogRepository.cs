using AppointmentService.Domain.Entities;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class BookingSagaLogRepository : IBookingSagaLogRepository
{
    private readonly AppointmentDbContext _context;
    public BookingSagaLogRepository(AppointmentDbContext context) => _context = context;

    public void Add(BookingSagaLog log)
        => _context.BookingSagaLogs.Add(log);

    public Task AddAsync(BookingSagaLog log, CancellationToken ct = default)
        => _context.BookingSagaLogs.AddAsync(log, ct).AsTask();

    public Task<List<BookingSagaLog>> GetBySagaIdAsync(Guid sagaId, CancellationToken ct = default)
        => _context.BookingSagaLogs
            .Where(l => l.SagaId == sagaId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
