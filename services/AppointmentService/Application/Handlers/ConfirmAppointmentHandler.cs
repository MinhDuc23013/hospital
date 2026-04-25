using AppointmentService.Application.Commands;
using AppointmentService.Domain.Exceptions;
using AppointmentService.Infrastructure.Repositories;
using MediatR;

namespace AppointmentService.Application.Handlers;

/// <summary>Confirms an appointment — called by OrchestratorService after the async saga phase succeeds.</summary>
public class ConfirmAppointmentHandler : IRequestHandler<ConfirmAppointmentCommand>
{
    private readonly IAppointmentRepository _repo;

    public ConfirmAppointmentHandler(IAppointmentRepository repo) => _repo = repo;

    public async Task Handle(ConfirmAppointmentCommand request, CancellationToken ct)
    {
        var appointment = await _repo.GetByIdAsync(request.AppointmentId, ct)
            ?? throw new NotFoundException("Appointment", request.AppointmentId);

        appointment.Confirm();
        await _repo.SaveChangesAsync(ct);
    }
}
