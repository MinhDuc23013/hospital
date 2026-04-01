using PharmacyServiceDotnet.Domain.Enums;
using PharmacyServiceDotnet.Domain.Exceptions;

namespace PharmacyServiceDotnet.Domain.Entities;

/// <summary>Prescription entity — represents a doctor's medication order for a patient.</summary>
public class Prescription
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public string DoctorId { get; private set; } = string.Empty;
    public Guid? AppointmentId { get; private set; }
    public PrescriptionStatus Status { get; private set; }
    public string? Notes { get; private set; }
    /// <summary>JSON-serialized list of PrescriptionItem: [{drugId, drugName, quantity, dosage}]</summary>
    public string Items { get; private set; } = "[]";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Prescription() { } // EF Core

    public static Prescription Create(Guid patientId, string doctorId, Guid? appointmentId, string items, string? notes)
    {
        return new Prescription
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentId = appointmentId,
            Status = PrescriptionStatus.Pending,
            Items = items,
            Notes = notes,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>Transitions the prescription from Pending to Dispensed.</summary>
    public void Dispense()
    {
        if (Status != PrescriptionStatus.Pending)
            throw new DomainException($"Cannot dispense a prescription with status '{Status}'.");

        Status = PrescriptionStatus.Dispensed;
        UpdatedAt = DateTime.Now;
    }

    /// <summary>Cancels the prescription (only from Pending status).</summary>
    public void Cancel()
    {
        if (Status != PrescriptionStatus.Pending)
            throw new DomainException($"Cannot cancel a prescription with status '{Status}'.");

        Status = PrescriptionStatus.Cancelled;
        UpdatedAt = DateTime.Now;
    }
}
