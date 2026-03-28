using AppointmentService.Application.Commands;
using AppointmentService.Domain.Exceptions;
using AppointmentService.Infrastructure.Repositories;
using MediatR;

namespace AppointmentService.Application.Handlers;

public class CancelAppointmentHandler : IRequestHandler<CancelAppointmentCommand, bool>
{
    private readonly IAppointmentRepository _repo;
    public CancelAppointmentHandler(IAppointmentRepository repo) => _repo = repo;

    public async Task<bool> Handle(CancelAppointmentCommand cmd, CancellationToken ct)
    {
        var appointment = await _repo.GetByIdAsync(cmd.AppointmentId, ct)
            ?? throw new NotFoundException("Appointment", cmd.AppointmentId);

        appointment.Cancel();
        await _repo.SaveChangesAsync(ct);
        return true;
    }
}
