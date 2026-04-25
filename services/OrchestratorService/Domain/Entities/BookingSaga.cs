using OrchestratorService.Domain.Enums;

namespace OrchestratorService.Domain.Entities;

/// <summary>Saga state entity tracking the multi-step appointment booking process.</summary>
public class BookingSaga
{
    public Guid Id { get; private set; }

    // Input data
    public Guid PatientId { get; private set; }
    public string DoctorId { get; private set; } = string.Empty;
    public Guid ScheduleId { get; private set; }
    public Guid SlotId { get; private set; }
    public DateTime ScheduledTime { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? Notes { get; private set; }

    // Step results (populated as saga progresses)
    public Guid? AppointmentId { get; private set; }

    // State tracking
    public BookingSagaStep CurrentStep { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private BookingSaga() { } // EF Core

    public static BookingSaga Create(
        Guid patientId, string providerId,
        Guid scheduleId, Guid slotId,
        DateTime scheduledTime, int durationMinutes,
        string? notes = null)
    {
        return new BookingSaga
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = providerId,
            ScheduleId = scheduleId,
            SlotId = slotId,
            ScheduledTime = scheduledTime,
            DurationMinutes = durationMinutes,
            Notes = notes,
            CurrentStep = BookingSagaStep.Started,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    public void MarkAppointmentCreated(Guid appointmentId)
    {
        AppointmentId = appointmentId;
        AdvanceTo(BookingSagaStep.AppointmentCreated);
    }

    public void MarkSlotReserved() => AdvanceTo(BookingSagaStep.SlotReserved);

    // ── Async phase mark methods ─────────────────────────────────────────
    public void MarkBookingConfirmed() => AdvanceTo(BookingSagaStep.BookingConfirmed);
    public void MarkNotificationSent() => AdvanceTo(BookingSagaStep.NotificationSent);
    public void MarkSearchIndexed() => AdvanceTo(BookingSagaStep.SearchIndexed);
    public void MarkBookingCompleted() => AdvanceTo(BookingSagaStep.Completed);

    public void MarkFailed(string reason)
    {
        FailureReason = reason;
        CurrentStep = BookingSagaStep.Failed;
        UpdatedAt = DateTime.Now;
    }

    public void MarkCompensating()
    {
        CurrentStep = BookingSagaStep.Compensating;
        UpdatedAt = DateTime.Now;
    }

    public void MarkCompensated()
    {
        CurrentStep = BookingSagaStep.Compensated;
        UpdatedAt = DateTime.Now;
    }

    private void AdvanceTo(BookingSagaStep step)
    {
        CurrentStep = step;
        UpdatedAt = DateTime.Now;
    }
}
