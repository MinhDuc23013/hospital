using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Repositories;

public class BookingSagaRepository : IBookingSagaRepository
{
    private readonly AppointmentDbContext _context;
    public BookingSagaRepository(AppointmentDbContext context) => _context = context;

    public Task<BookingSaga?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.BookingSagas.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<BookingSaga?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        => _context.BookingSagas.FirstOrDefaultAsync(s => s.AppointmentId == appointmentId, ct);

    /// <summary>Get the active (non-terminal) saga for an appointment.</summary>
    public Task<BookingSaga?> GetActiveByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        => _context.BookingSagas.FirstOrDefaultAsync(s =>
            s.AppointmentId == appointmentId
            && s.CurrentStep != BookingSagaStep.Compensated
            && s.CurrentStep != BookingSagaStep.Failed, ct);

    public Task<BookingSaga?> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct = default)
        => _context.BookingSagas.FirstOrDefaultAsync(s => s.PaymentId == paymentId, ct);

    /// <summary>
    /// Fetches sagas stuck at a given step past a cutoff, applying a pessimistic
    /// row lock (FOR UPDATE SKIP LOCKED) so only one replica processes each row.
    /// MUST be called inside an open DB transaction — otherwise the lock is released
    /// immediately and parallel workers will duplicate compensation.
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
