using DoctorScheduleService.Application.Queries;
using DoctorScheduleService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class GetDoctorHandler : IRequestHandler<GetDoctorQuery, DoctorDto?>
{
    private readonly IDoctorRepository _repo;
    public GetDoctorHandler(IDoctorRepository repo) => _repo = repo;

    public async Task<DoctorDto?> Handle(GetDoctorQuery query, CancellationToken ct)
    {
        var doctor = await _repo.GetByIdAsync(query.Id, ct);
        return doctor is null ? null : DoctorMapper.ToDto(doctor);
    }
}
