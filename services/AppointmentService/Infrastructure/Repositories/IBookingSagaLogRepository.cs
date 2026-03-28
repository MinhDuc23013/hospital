using AppointmentService.Domain.Entities;

namespace AppointmentService.Infrastructure.Repositories;

public interface IBookingSagaLogRepository
{
    Task AddAsync(BookingSagaLog log, CancellationToken ct = default);
    Task<List<BookingSagaLog>> GetBySagaIdAsync(Guid sagaId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
