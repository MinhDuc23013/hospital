namespace HospitalShared.Events;

/// <summary>Published when drug inventory falls below minimum threshold.</summary>
public class InventoryLowEvent
{
    public string DrugId { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
