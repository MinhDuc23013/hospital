using AppointmentService.Domain.Enums;

namespace AppointmentService.Domain.Entities;

/// <summary>Saga state entity tracking the multi-step appointment booking process.</summary>
public class BookingSaga
{
    public Guid Id { get; private set; }

    // Input data
    public Guid PatientId { get; private set; }
    public string ProviderId { get; private set; } = string.Empty;
    public Guid ScheduleId { get; private set; }
    public Guid SlotId { get; private set; }
    public decimal PaymentAmount { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    // Step results (populated as saga progresses)
    public Guid? AppointmentId { get; private set; }
    public Guid? PaymentId { get; private set; }

    // State tracking
    public BookingSagaStep CurrentStep { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private BookingSaga() { } // EF Core

    public static BookingSaga Create(
        Guid patientId, string providerId,
        Guid scheduleId, Guid slotId,
        decimal paymentAmount, string paymentMethod, string? notes = null)
    {
        return new BookingSaga
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            ProviderId = providerId,
            ScheduleId = scheduleId,
            SlotId = slotId,
            PaymentAmount = paymentAmount,
            PaymentMethod = paymentMethod,
            Notes = notes,
            CurrentStep = BookingSagaStep.Started,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarkAppointmentCreated(Guid appointmentId)
    {
        AppointmentId = appointmentId;
        AdvanceTo(BookingSagaStep.AppointmentCreated);
    }

    public void MarkSlotReserved() => AdvanceTo(BookingSagaStep.SlotReserved);

    public void MarkPaymentProcessed(Guid paymentId)
    {
        PaymentId = paymentId;
        AdvanceTo(BookingSagaStep.PaymentProcessed);
    }

    public void MarkSlotConfirmed() => AdvanceTo(BookingSagaStep.SlotConfirmed);
    public void MarkCompleted() => AdvanceTo(BookingSagaStep.Completed);

    public void MarkFailed(string reason)
    {
        FailureReason = reason;
        CurrentStep = BookingSagaStep.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompensating()
    {
        CurrentStep = BookingSagaStep.Compensating;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompensated()
    {
        CurrentStep = BookingSagaStep.Compensated;
        UpdatedAt = DateTime.UtcNow;
    }

    private void AdvanceTo(BookingSagaStep step)
    {
        CurrentStep = step;
        UpdatedAt = DateTime.UtcNow;
    }
}
