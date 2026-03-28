using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Commands;

public record ConfirmSlotCommand(
    Guid ScheduleId,
    Guid SlotId,
    Guid AppointmentId
) : IRequest<TimeSlotDto>;
