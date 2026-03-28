using AppointmentService.Domain.Entities;

namespace AppointmentService.Infrastructure.Repositories;

public interface IBookingSagaRepository
{
    Task<BookingSaga?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(BookingSaga saga, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
