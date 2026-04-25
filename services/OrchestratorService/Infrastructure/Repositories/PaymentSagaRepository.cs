using Microsoft.EntityFrameworkCore;
using OrchestratorService.Domain.Entities;
using OrchestratorService.Domain.Enums;
using OrchestratorService.Infrastructure.Persistence;

namespace OrchestratorService.Infrastructure.Repositories;

public class PaymentSagaRepository : IPaymentSagaRepository
{
    private readonly OrchestratorDbContext _context;
    public PaymentSagaRepository(OrchestratorDbContext context) => _context = context;

    public Task<PaymentSaga?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.PaymentSagas.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<PaymentSaga?> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct = default)
        => _context.PaymentSagas.FirstOrDefaultAsync(s => s.PaymentId == paymentId, ct);

    public Task<PaymentSaga?> GetActiveByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        => _context.PaymentSagas.FirstOrDefaultAsync(s =>
            s.AppointmentId == appointmentId
            && s.CurrentStep != PaymentSagaStep.Failed, ct);

    public Task AddAsync(PaymentSaga saga, CancellationToken ct = default)
        => _context.PaymentSagas.AddAsync(saga, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
