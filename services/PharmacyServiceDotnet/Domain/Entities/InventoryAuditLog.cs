using HospitalShared;
using PharmacyServiceDotnet.Domain.Enums;

namespace PharmacyServiceDotnet.Domain.Entities;

/// <summary>
/// Immutable audit log for all inventory movements — required for pharmaceutical compliance.
/// Records who did what, when, for which patient, from which batch.
/// </summary>
public class InventoryAuditLog
{
    public Guid Id { get; private set; }
    public AuditAction Action { get; private set; }
    public Guid DrugId { get; private set; }
    public Guid? DrugBatchId { get; private set; }
    public string? BatchNumber { get; private set; }
    public Guid? PrescriptionId { get; private set; }
    public Guid? PatientId { get; private set; }
    public string? UserId { get; private set; }
    public int Quantity { get; private set; }
    public int? OldQuantity { get; private set; }
    public int? NewQuantity { get; private set; }
    public string? Details { get; private set; }
    public DateTime Timestamp { get; private set; }

    private InventoryAuditLog() { }

    public static InventoryAuditLog Create(
        AuditAction action, Guid drugId, int quantity,
        Guid? drugBatchId = null, string? batchNumber = null,
        Guid? prescriptionId = null, Guid? patientId = null,
        string? userId = null, int? oldQty = null, int? newQty = null,
        string? details = null)
    {
        return new InventoryAuditLog
        {
            Id = GuidV7.NewGuid(),
            Action = action,
            DrugId = drugId,
            DrugBatchId = drugBatchId,
            BatchNumber = batchNumber,
            PrescriptionId = prescriptionId,
            PatientId = patientId,
            UserId = userId,
            Quantity = quantity,
            OldQuantity = oldQty,
            NewQuantity = newQty,
            Details = details,
            Timestamp = DateTime.Now
        };
    }
}
