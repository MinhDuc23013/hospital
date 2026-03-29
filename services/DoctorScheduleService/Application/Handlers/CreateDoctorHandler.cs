using DoctorScheduleService.Application.Commands;
using DoctorScheduleService.Domain.Entities;
using DoctorScheduleService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class CreateDoctorHandler : IRequestHandler<CreateDoctorCommand, DoctorDto>
{
    private readonly IDoctorRepository _repo;
    public CreateDoctorHandler(IDoctorRepository repo) => _repo = repo;

    public async Task<DoctorDto> Handle(CreateDoctorCommand cmd, CancellationToken ct)
    {
        var doctor = Doctor.Create(cmd.FullName, cmd.Specialty, cmd.Phone, cmd.Email);
        await _repo.AddAsync(doctor, ct);
        await _repo.SaveChangesAsync(ct);
        return DoctorMapper.ToDto(doctor);
    }
}
