using MediatR;

namespace OrchestratorService.Application.Commands;

/// <summary>Command to trigger the booking saga: validate patient → create appointment → reserve slot → async confirm + notify + index.</summary>
public record BookAppointmentCommand(
    Guid PatientId,
    string DoctorId,
    Guid ScheduleId,
    Guid SlotId,
    DateTime ScheduledTime,
    int DurationMinutes,
    string? Notes = null
) : IRequest<BookAppointmentResult>;

/// <summary>Result of the booking saga sync phase.</summary>
public record BookAppointmentResult(
    Guid SagaId,
    Guid? AppointmentId,
    string Status,
    string? FailureReason,
    string? TraceId
);
