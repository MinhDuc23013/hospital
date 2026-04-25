using OrchestratorService.Domain.Entities;

namespace OrchestratorService.Infrastructure.Repositories;

public interface IPaymentSagaRepository
{
    Task<PaymentSaga?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaymentSaga?> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct = default);
    Task<PaymentSaga?> GetActiveByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default);
    Task AddAsync(PaymentSaga saga, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
