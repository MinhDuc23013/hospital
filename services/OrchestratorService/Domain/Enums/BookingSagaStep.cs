namespace OrchestratorService.Domain.Enums;

/// <summary>Tracks the current step of the booking saga orchestration.</summary>
public enum BookingSagaStep
{
    // ── Sync phase (blocks HTTP, returns 202 after SlotReserved) ─────────
    Started = 0,
    AppointmentCreated = 1,
    SlotReserved = 2,           // Slot locked → sync phase complete, 202 returned

    // ── Legacy payment flow (kept for backward compat) ───────────────────
    PaymentCreated = 3,
    SlotConfirmed = 4,
    AwaitingPayment = 5,
    PaymentCompleted = 6,

    // ── Async phase (background, triggered via Kafka after SlotReserved) ──
    BookingConfirmed = 7,       // Slot confirmed in DoctorScheduleService
    NotificationSent = 8,       // Notification published to RabbitMQ
    SearchIndexed = 9,          // AppointmentBookedEvent published for Elasticsearch

    // ── Terminal states ───────────────────────────────────────────────────
    Completed = 13,
    Failed = 10,
    Compensating = 11,
    Compensated = 12
}
