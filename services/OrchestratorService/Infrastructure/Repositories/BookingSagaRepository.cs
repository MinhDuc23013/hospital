using Microsoft.EntityFrameworkCore;
using OrchestratorService.Domain.Entities;
using OrchestratorService.Domain.Enums;
using OrchestratorService.Infrastructure.Persistence;

namespace OrchestratorService.Infrastructure.Repositories;

public class BookingSagaRepository : IBookingSagaRepository
{
    private readonly OrchestratorDbContext _context;
    public BookingSagaRepository(OrchestratorDbContext context) => _context = context;

    public Task<BookingSaga?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.BookingSagas.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<BookingSaga?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        => _context.BookingSagas.FirstOrDefaultAsync(s => s.AppointmentId == appointmentId, ct);

    public Task<BookingSaga?> GetActiveByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        => _context.BookingSagas.FirstOrDefaultAsync(s =>
            s.AppointmentId == appointmentId
            && s.CurrentStep != BookingSagaStep.Compensated
            && s.CurrentStep != BookingSagaStep.Failed, ct);

    public Task<BookingSaga?> GetActiveBySlotAsync(Guid patientId, string doctorId, Guid slotId, CancellationToken ct = default)
        => _context.BookingSagas.FirstOrDefaultAsync(s =>
            s.PatientId == patientId
            && s.DoctorId == doctorId
            && s.SlotId == slotId
            && s.CurrentStep != BookingSagaStep.Failed
            && s.CurrentStep != BookingSagaStep.Compensated
            && s.CurrentStep != BookingSagaStep.Completed, ct);

    /// <summary>
    /// Fetches sagas stuck at a given step past a cutoff, with pessimistic row lock
    /// (FOR UPDATE SKIP LOCKED) so only one replica processes each row.
    /// MUST be called inside an open DB transaction.
    /// </summary>
    public Task<List<BookingSaga>> GetByStepOlderThanAsync(BookingSagaStep step, DateTime cutoff, CancellationToken ct = default)
        => _context.BookingSagas
            .FromSqlRaw(
                @"SELECT * FROM booking_sagas
                  WHERE ""CurrentStep"" = {0}
                    AND ""UpdatedAt"" <= {1}
                  ORDER BY ""UpdatedAt""
                  LIMIT 50
                  FOR UPDATE SKIP LOCKED",
                step.ToString(), cutoff)
            .ToListAsync(ct);

    public Task AddAsync(BookingSaga saga, CancellationToken ct = default)
        => _context.BookingSagas.AddAsync(saga, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
