using ImagingService.Domain.Enums;

namespace ImagingService.Domain.Entities;

/// <summary>Imaging/CĐHA order created by a doctor for a patient (X-ray, CT, MRI, etc.).</summary>
public class ImagingOrder
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public string DoctorId { get; private set; } = string.Empty;
    public ImagingType Type { get; private set; }
    public string BodyPart { get; private set; } = string.Empty;
    public string? ClinicalHistory { get; private set; }
    public ImagingOrderStatus Status { get; private set; }
    public DateTime OrderedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public decimal Price { get; private set; }
    public ImagingResult? Result { get; private set; }

    private ImagingOrder() { }

    public static ImagingOrder Create(
        Guid patientId, Guid appointmentId, string doctorId,
        ImagingType type, string bodyPart, string? clinicalHistory, decimal price = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            AppointmentId = appointmentId,
            DoctorId = doctorId,
            Type = type,
            BodyPart = bodyPart,
            ClinicalHistory = clinicalHistory,
            Price = price,
            Status = ImagingOrderStatus.Pending,
            OrderedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void MarkInProgress()
    {
        Status = ImagingOrderStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SubmitResult(string findings, string impression, string? imageUrl, string reportedBy)
    {
        Result = ImagingResult.Create(Id, findings, impression, imageUrl, reportedBy);
        Status = ImagingOrderStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = ImagingOrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}
