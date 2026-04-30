using HospitalShared;
namespace PharmacyServiceDotnet.Domain.Entities;

/// <summary>
/// Represents a specific batch/lot of a drug with expiry tracking.
/// Each drug can have multiple batches with different expiry dates.
/// Stock deduction follows FEFO (First Expired, First Out).
/// </summary>
public class DrugBatch
{
    public Guid Id { get; private set; }
    public Guid DrugId { get; private set; }
    public string BatchNumber { get; private set; } = string.Empty;
    public DateTime ExpiryDate { get; private set; }
    public int Quantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public DateTime ReceivedDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>EF Core concurrency token — PostgreSQL xmin system column.</summary>
    public uint RowVersion { get; private set; }

    private DrugBatch() { }

    public static DrugBatch Create(Guid drugId, string batchNumber, DateTime expiryDate, int quantity, DateTime? receivedDate = null)
    {
        return new DrugBatch
        {
            Id = GuidV7.NewGuid(),
            DrugId = drugId,
            BatchNumber = batchNumber,
            ExpiryDate = expiryDate,
            Quantity = quantity,
            ReservedQuantity = 0,
            ReceivedDate = receivedDate ?? DateTime.Now,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>Available stock = total quantity minus reserved.</summary>
    public int AvailableQuantity => Quantity - ReservedQuantity;

    /// <summary>Reserve stock from this batch. Returns false if insufficient available quantity.</summary>
    public bool Reserve(int qty)
    {
        if (qty > AvailableQuantity) return false;
        ReservedQuantity += qty;
        UpdatedAt = DateTime.Now;
        return true;
    }

    /// <summary>Commit reserved stock — deducts from both quantity and reserved.</summary>
    public void Commit(int qty)
    {
        Quantity -= qty;
        ReservedQuantity -= qty;
        UpdatedAt = DateTime.Now;
    }

    /// <summary>Release previously reserved stock.</summary>
    public void Release(int qty)
    {
        ReservedQuantity -= qty;
        if (ReservedQuantity < 0) ReservedQuantity = 0;
        UpdatedAt = DateTime.Now;
    }

    public bool IsExpired => ExpiryDate <= DateTime.Now;
}
