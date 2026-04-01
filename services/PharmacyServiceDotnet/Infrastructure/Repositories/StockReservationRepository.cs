using Microsoft.EntityFrameworkCore;
using PharmacyServiceDotnet.Domain.Entities;
using PharmacyServiceDotnet.Domain.Enums;
using PharmacyServiceDotnet.Infrastructure.Persistence;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public class StockReservationRepository : IStockReservationRepository
{
    private readonly PharmacyDbContext _context;
    public StockReservationRepository(PharmacyDbContext context) => _context = context;

    public Task<List<StockReservation>> GetByPrescriptionIdAsync(Guid prescriptionId, CancellationToken ct = default)
        => _context.StockReservations
            .Where(r => r.PrescriptionId == prescriptionId)
            .ToListAsync(ct);

    public Task<List<StockReservation>> GetExpiredReservationsAsync(CancellationToken ct = default)
        => _context.StockReservations
            .Where(r => r.Status == ReservationStatus.Reserved && r.ExpiresAt < DateTime.Now)
            .ToListAsync(ct);

    public Task AddAsync(StockReservation reservation, CancellationToken ct = default)
        => _context.StockReservations.AddAsync(reservation, ct).AsTask();

    public Task AddRangeAsync(IEnumerable<StockReservation> reservations, CancellationToken ct = default)
        => _context.StockReservations.AddRangeAsync(reservations, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
