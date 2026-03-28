using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Commands;

public record ReserveSlotCommand(
    Guid ScheduleId,
    Guid SlotId,
    Guid PatientId
) : IRequest<TimeSlotDto>;
