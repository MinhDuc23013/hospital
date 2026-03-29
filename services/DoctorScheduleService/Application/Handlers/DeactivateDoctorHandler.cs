using DoctorScheduleService.Application.Commands;
using DoctorScheduleService.Domain.Exceptions;
using DoctorScheduleService.Infrastructure.Repositories;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class DeactivateDoctorHandler : IRequestHandler<DeactivateDoctorCommand, bool>
{
    private readonly IDoctorRepository _repo;
    public DeactivateDoctorHandler(IDoctorRepository repo) => _repo = repo;

    public async Task<bool> Handle(DeactivateDoctorCommand cmd, CancellationToken ct)
    {
        var doctor = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException("Doctor", cmd.Id);

        doctor.Deactivate();
        await _repo.SaveChangesAsync(ct);
        return true;
    }
}
