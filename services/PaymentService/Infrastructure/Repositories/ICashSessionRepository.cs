using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Repositories;

public interface ICashSessionRepository
{
    Task<CashSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CashSession?> GetOpenSessionByCashierAsync(string cashierId, CancellationToken ct = default);
    Task AddAsync(CashSession session, CancellationToken ct = default);
    Task<string> GenerateReceiptNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
