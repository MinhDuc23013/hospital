using MediatR;

namespace DoctorScheduleService.Application.Commands;

public record DeactivateDoctorCommand(Guid Id) : IRequest<bool>;
