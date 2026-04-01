namespace HospitalShared.Events;

/// <summary>Published when a drug batch is approaching its expiry date.</summary>
public class DrugBatchNearExpiryEvent
{
    public string DrugId { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int RemainingQuantity { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
