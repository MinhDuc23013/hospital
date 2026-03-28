namespace AppointmentService.Domain.Enums;

/// <summary>Tracks the current step of the booking saga orchestration.</summary>
public enum BookingSagaStep
{
    Started = 0,
    AppointmentCreated = 1,
    SlotReserved = 2,
    PaymentProcessed = 3,
    SlotConfirmed = 4,
    Completed = 5,
    Failed = 10,
    Compensating = 11,
    Compensated = 12
}
