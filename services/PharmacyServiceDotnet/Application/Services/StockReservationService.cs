using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Domain.Entities;
using PharmacyServiceDotnet.Domain.Enums;
using PharmacyServiceDotnet.Domain.Exceptions;
using PharmacyServiceDotnet.Infrastructure.Repositories;

namespace PharmacyServiceDotnet.Application.Services;

/// <summary>
/// Core service for FEFO-based stock reservation, commit, and release.
/// Handles splitting quantities across multiple batches when a single batch is insufficient.
/// </summary>
public class StockReservationService
{
    private readonly IDrugBatchRepository _batchRepo;
    private readonly IStockReservationRepository _reservationRepo;
    private readonly IInventoryAuditLogRepository _auditRepo;
    private readonly ILogger<StockReservationService> _logger;

    public StockReservationService(
        IDrugBatchRepository batchRepo,
        IStockReservationRepository reservationRepo,
        IInventoryAuditLogRepository auditRepo,
        ILogger<StockReservationService> logger)
    {
        _batchRepo = batchRepo;
        _reservationRepo = reservationRepo;
        _auditRepo = auditRepo;
        _logger = logger;
    }

    /// <summary>
    /// Reserves stock for prescription items using FEFO (First Expired, First Out).
    /// Splits across batches if needed. Throws if insufficient stock.
    /// </summary>
    public async Task<List<StockReservation>> ReserveAsync(
        Guid prescriptionId, Guid patientId, List<PrescriptionItem> items, CancellationToken ct)
    {
        var reservations = new List<StockReservation>();

        foreach (var item in items)
        {
            var batches = await _batchRepo.GetAvailableBatchesFEFOAsync(item.DrugId, ct);
            var remaining = item.Quantity;

            foreach (var batch in batches)
            {
                if (remaining <= 0) break;

                var toReserve = Math.Min(remaining, batch.AvailableQuantity);
                if (toReserve <= 0) continue;

                if (!batch.Reserve(toReserve))
                    continue; // concurrent modification, skip this batch

                var reservation = StockReservation.Create(prescriptionId, batch.Id, item.DrugId, toReserve);
                reservations.Add(reservation);

                var audit = InventoryAuditLog.Create(
                    AuditAction.Reserved, item.DrugId, toReserve,
                    drugBatchId: batch.Id, batchNumber: batch.BatchNumber,
                    prescriptionId: prescriptionId, patientId: patientId,
                    details: $"Reserved {toReserve} from batch {batch.BatchNumber} (expiry {batch.ExpiryDate:yyyy-MM-dd})");
                await _auditRepo.AddAsync(audit, ct);

                remaining -= toReserve;
            }

            if (remaining > 0)
                throw new DomainException($"Insufficient stock for drug {item.DrugName} (ID: {item.DrugId}). Need {item.Quantity}, available {item.Quantity - remaining}.");
        }

        await _reservationRepo.AddRangeAsync(reservations, ct);
        _logger.LogInformation("Reserved {Count} batch slots for prescription {PrescriptionId}", reservations.Count, prescriptionId);
        return reservations;
    }

    /// <summary>Commits all reservations for a prescription — deducts actual stock.</summary>
    public async Task CommitAsync(Guid prescriptionId, Guid patientId, CancellationToken ct)
    {
        var reservations = await _reservationRepo.GetByPrescriptionIdAsync(prescriptionId, ct);
        foreach (var reservation in reservations.Where(r => r.Status == ReservationStatus.Reserved))
        {
            var batch = await _batchRepo.GetByIdAsync(reservation.DrugBatchId, ct);
            if (batch is null) continue;

            batch.Commit(reservation.Quantity);
            reservation.Commit();

            var audit = InventoryAuditLog.Create(
                AuditAction.Committed, reservation.DrugId, reservation.Quantity,
                drugBatchId: batch.Id, batchNumber: batch.BatchNumber,
                prescriptionId: prescriptionId, patientId: patientId,
                oldQty: batch.Quantity + reservation.Quantity, newQty: batch.Quantity,
                details: $"Committed {reservation.Quantity} from batch {batch.BatchNumber}");
            await _auditRepo.AddAsync(audit, ct);
        }

        _logger.LogInformation("Committed reservations for prescription {PrescriptionId}", prescriptionId);
    }

    /// <summary>Releases all reservations for a prescription — returns stock to available pool.</summary>
    public async Task ReleaseAsync(Guid prescriptionId, Guid patientId, CancellationToken ct)
    {
        var reservations = await _reservationRepo.GetByPrescriptionIdAsync(prescriptionId, ct);
        foreach (var reservation in reservations.Where(r => r.Status == ReservationStatus.Reserved))
        {
            var batch = await _batchRepo.GetByIdAsync(reservation.DrugBatchId, ct);
            if (batch is null) continue;

            batch.Release(reservation.Quantity);
            reservation.Release();

            var audit = InventoryAuditLog.Create(
                AuditAction.Released, reservation.DrugId, reservation.Quantity,
                drugBatchId: batch.Id, batchNumber: batch.BatchNumber,
                prescriptionId: prescriptionId, patientId: patientId,
                details: $"Released {reservation.Quantity} from batch {batch.BatchNumber}");
            await _auditRepo.AddAsync(audit, ct);
        }

        _logger.LogInformation("Released reservations for prescription {PrescriptionId}", prescriptionId);
    }
}
