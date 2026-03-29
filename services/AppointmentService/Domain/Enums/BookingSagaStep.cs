namespace AppointmentService.Domain.Enums;

/// <summary>Tracks the current step of the booking saga orchestration.</summary>
public enum BookingSagaStep
{
    Started = 0,
    AppointmentCreated = 1,
    SlotReserved = 2,
    PaymentCreated = 3,
    SlotConfirmed = 4,
    AwaitingPayment = 5,   // Waiting for external payment confirmation
    Completed = 6,
    Failed = 10,
    Compensating = 11,
    Compensated = 12
}
