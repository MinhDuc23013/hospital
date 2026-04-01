using System.Text.Json;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Application.Handlers;

/// <summary>Maps domain entities to read-model result records.</summary>
public static class PharmacyMapper
{
    public static DrugResult ToResult(Drug d) => new(
        d.Id, d.Name, d.Code, d.Dosage, d.Quantity, d.Price, d.LowStockThreshold, d.CreatedAt, d.UpdatedAt);

    private static readonly JsonSerializerOptions CaseInsensitiveJson = new() { PropertyNameCaseInsensitive = true };

    public static PrescriptionResult ToResult(Prescription p)
    {
        var items = JsonSerializer.Deserialize<List<PrescriptionItem>>(p.Items, CaseInsensitiveJson) ?? [];
        return new(p.Id, p.PatientId, p.DoctorId, p.AppointmentId,
            p.Status.ToString(), p.Notes, items, p.CreatedAt, p.UpdatedAt);
    }

    public static DrugBatchResult ToResult(DrugBatch b) => new(
        b.Id, b.DrugId, b.BatchNumber, b.ExpiryDate,
        b.Quantity, b.ReservedQuantity, b.AvailableQuantity,
        b.ReceivedDate, b.CreatedAt);

    public static DispensingSagaResult ToResult(DispensingSaga s) => new(
        s.Id, s.PrescriptionId, s.PatientId, s.DoctorId,
        s.CurrentStep.ToString(), s.PaymentId, s.FailureReason,
        s.CreatedAt, s.UpdatedAt);
}
