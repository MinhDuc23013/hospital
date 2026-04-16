using AppointmentService.Domain.Entities;

namespace AppointmentService.Infrastructure.Repositories;

public interface IBookingSagaLogRepository
{
    void Add(BookingSagaLog log);
    Task AddAsync(BookingSagaLog log, CancellationToken ct = default);
    Task<List<BookingSagaLog>> GetBySagaIdAsync(Guid sagaId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
