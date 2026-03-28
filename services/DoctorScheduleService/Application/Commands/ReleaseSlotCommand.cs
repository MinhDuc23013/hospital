using MediatR;

namespace DoctorScheduleService.Application.Commands;

public record ReleaseSlotCommand(Guid ScheduleId, Guid SlotId) : IRequest<bool>;
