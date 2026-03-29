using MediatR;

namespace AppointmentService.Application.Commands;

/// <summary>Command to trigger the full booking saga: create appointment → reserve slot → pay → confirm → notify.</summary>
public record BookAppointmentCommand(
    Guid PatientId,
    string DoctorId,
    Guid ScheduleId,
    Guid SlotId,
    DateTime ScheduledTime,
    int DurationMinutes,
    decimal PaymentAmount,
    string PaymentMethod,
    string Currency = "VND",
    string? Notes = null
) : IRequest<BookAppointmentResult>;

/// <summary>Result of the booking saga with saga state and appointment details.</summary>
public record BookAppointmentResult(
    Guid SagaId,
    Guid? AppointmentId,
    Guid? PaymentId,
    string Status,
    string? FailureReason,
    string? TraceId
);
