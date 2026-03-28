namespace HospitalShared.Constants;

/// <summary>Event type name constants used for RabbitMQ routing and message identification.</summary>
public static class EventTypes
{
    public const string PatientCreated = "hospital.patient.created";
    public const string PatientUpdated = "hospital.patient.updated";
    public const string AppointmentScheduled = "hospital.appointment.scheduled";
    public const string AppointmentCancelled = "hospital.appointment.cancelled";
    public const string PrescriptionIssued = "hospital.prescription.issued";
    public const string InventoryLow = "hospital.pharmacy.inventory_low";
    public const string NotificationSent = "hospital.notification.sent";
    public const string SlotReserved = "hospital.schedule.slot_reserved";
    public const string SlotReleased = "hospital.schedule.slot_released";
    public const string PaymentCompleted = "hospital.payment.completed";
    public const string PaymentFailed = "hospital.payment.failed";
}
