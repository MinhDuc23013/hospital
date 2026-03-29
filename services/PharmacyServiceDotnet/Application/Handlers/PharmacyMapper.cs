using System.Text.Json;
using PharmacyServiceDotnet.Application;
using PharmacyServiceDotnet.Domain.Entities;

namespace PharmacyServiceDotnet.Application.Handlers;

/// <summary>Maps domain entities to read-model result records.</summary>
public static class PharmacyMapper
{
    public static DrugResult ToResult(Drug d) => new(
        d.Id, d.Name, d.Code, d.Dosage, d.Quantity, d.Price, d.LowStockThreshold, d.CreatedAt, d.UpdatedAt);

    public static PrescriptionResult ToResult(Prescription p)
    {
        var items = JsonSerializer.Deserialize<List<PrescriptionItem>>(p.Items) ?? [];
        return new(p.Id, p.PatientId, p.DoctorId, p.AppointmentId,
            p.Status.ToString(), p.Notes, items, p.CreatedAt, p.UpdatedAt);
    }
}
