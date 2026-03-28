using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Repositories;

public interface IPaymentAuditLogRepository
{
    Task AddAsync(PaymentAuditLog log, CancellationToken ct = default);
    Task<List<PaymentAuditLog>> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
