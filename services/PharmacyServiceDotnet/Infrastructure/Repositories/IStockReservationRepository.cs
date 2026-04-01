using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Infrastructure.Repositories;

public interface IStockReservationRepository
{
    Task<List<StockReservation>> GetByPrescriptionIdAsync(Guid prescriptionId, CancellationToken ct = default);
    Task<List<StockReservation>> GetExpiredReservationsAsync(CancellationToken ct = default);
    Task AddAsync(StockReservation reservation, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<StockReservation> reservations, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
