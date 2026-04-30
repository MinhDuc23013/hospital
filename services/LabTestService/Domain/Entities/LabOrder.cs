using HospitalShared;
using LabTestService.Domain.Enums;

namespace LabTestService.Domain.Entities;

/// <summary>Aggregate root for a lab test order placed by a doctor for a patient.</summary>
public class LabOrder
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public string DoctorId { get; private set; } = string.Empty;
    public LabOrderStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime OrderedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<LabOrderItem> _items = [];
    public IReadOnlyList<LabOrderItem> Items => _items.AsReadOnly();

    private LabOrder() { }

    public static LabOrder Create(
        Guid patientId,
        Guid appointmentId,
        string doctorId,
        string? notes,
        IEnumerable<(string testName, string testCode, string category, decimal unitPrice)> items)
    {
        var order = new LabOrder
        {
            Id = GuidV7.NewGuid(),
            PatientId = patientId,
            AppointmentId = appointmentId,
            DoctorId = doctorId,
            Notes = notes,
            Status = LabOrderStatus.Pending,
            OrderedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var (testName, testCode, category, unitPrice) in items)
            order._items.Add(LabOrderItem.Create(order.Id, testName, testCode, category, unitPrice));

        return order;
    }

    public void MarkInProgress() { Status = LabOrderStatus.InProgress; UpdatedAt = DateTime.UtcNow; }

    public void MarkCompleted() { Status = LabOrderStatus.Completed; UpdatedAt = DateTime.UtcNow; }

    public void Cancel() { Status = LabOrderStatus.Cancelled; UpdatedAt = DateTime.UtcNow; }
}
