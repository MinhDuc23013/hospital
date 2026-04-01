using PharmacyServiceDotnet.Domain.Enums;

namespace PharmacyServiceDotnet.Domain.Entities;

/// <summary>
/// Tracks a stock reservation for a specific drug batch tied to a prescription.
/// Lifecycle: Reserved → Committed (on payment success) or Released (on failure/timeout).
/// </summary>
public class StockReservation
{
    public Guid Id { get; private set; }
    public Guid PrescriptionId { get; private set; }
    public Guid DrugBatchId { get; private set; }
    public Guid DrugId { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private StockReservation() { }

    public static StockReservation Create(
        Guid prescriptionId, Guid drugBatchId, Guid drugId,
        int quantity, TimeSpan? ttl = null)
    {
        return new StockReservation
        {
            Id = Guid.NewGuid(),
            PrescriptionId = prescriptionId,
            DrugBatchId = drugBatchId,
            DrugId = drugId,
            Quantity = quantity,
            Status = ReservationStatus.Reserved,
            ExpiresAt = DateTime.Now.Add(ttl ?? TimeSpan.FromMinutes(30)),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    public void Commit()
    {
        Status = ReservationStatus.Committed;
        UpdatedAt = DateTime.Now;
    }

    public void Release()
    {
        Status = ReservationStatus.Released;
        UpdatedAt = DateTime.Now;
    }

    public bool IsExpired => Status == ReservationStatus.Reserved && DateTime.Now > ExpiresAt;
}
