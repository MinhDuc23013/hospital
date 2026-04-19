using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure.Repositories;

public class CashSessionRepository : ICashSessionRepository
{
    private readonly PaymentDbContext _context;

    public CashSessionRepository(PaymentDbContext context) => _context = context;

    public Task<CashSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.CashSessions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<CashSession?> GetOpenSessionByCashierAsync(string cashierId, CancellationToken ct = default)
        => _context.CashSessions
            .Where(s => s.CashierId == cashierId && s.Status == CashSessionStatus.Open)
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(CashSession session, CancellationToken ct = default)
        => await _context.CashSessions.AddAsync(session, ct);

    /// <summary>Generate sequential receipt number: HĐ/YYYY/NNNNN (padded 5 digits).</summary>
    public async Task<string> GenerateReceiptNumberAsync(CancellationToken ct = default)
    {
        var nextId = await _context.Database
            .SqlQueryRaw<long>("SELECT nextval('receipt_seq') AS \"Value\"")
            .FirstAsync(ct);
        return $"HD/{DateTime.Now.Year}/{nextId:D5}";
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
