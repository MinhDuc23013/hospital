using DoctorScheduleService.Application.Commands;
using DoctorScheduleService.Domain.Exceptions;
using DoctorScheduleService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class UpdateDoctorHandler : IRequestHandler<UpdateDoctorCommand, DoctorDto>
{
    private readonly IDoctorRepository _repo;
    public UpdateDoctorHandler(IDoctorRepository repo) => _repo = repo;

    public async Task<DoctorDto> Handle(UpdateDoctorCommand cmd, CancellationToken ct)
    {
        var doctor = await _repo.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException("Doctor", cmd.Id);

        doctor.Update(cmd.FullName, cmd.Specialty, cmd.Phone, cmd.Email);
        await _repo.SaveChangesAsync(ct);
        return DoctorMapper.ToDto(doctor);
    }
}
