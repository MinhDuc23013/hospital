using OrchestratorService.Domain.Entities;
using OrchestratorService.Domain.Enums;

namespace OrchestratorService.Infrastructure.Repositories;

public interface IBookingSagaRepository
{
    Task<BookingSaga?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BookingSaga?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default);
    Task<BookingSaga?> GetActiveByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default);
    Task<BookingSaga?> GetActiveBySlotAsync(Guid patientId, string doctorId, Guid slotId, CancellationToken ct = default);
    Task<List<BookingSaga>> GetByStepOlderThanAsync(BookingSagaStep step, DateTime cutoff, CancellationToken ct = default);
    Task AddAsync(BookingSaga saga, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
