using HospitalShared.DTOs;
using MediatR;

namespace AppointmentService.Application.Commands;

public record ScheduleAppointmentCommand(
    Guid PatientId,
    string DoctorId,
    DateTime ScheduledTime,
    int DurationMinutes,
    string? Notes
) : IRequest<AppointmentDto>;
