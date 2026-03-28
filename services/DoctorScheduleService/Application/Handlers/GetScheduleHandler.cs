using DoctorScheduleService.Application.Queries;
using DoctorScheduleService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class GetScheduleHandler : IRequestHandler<GetScheduleQuery, DoctorScheduleDto?>
{
    private readonly IDoctorScheduleRepository _repo;

    public GetScheduleHandler(IDoctorScheduleRepository repo) => _repo = repo;

    public async Task<DoctorScheduleDto?> Handle(GetScheduleQuery query, CancellationToken ct)
    {
        var schedule = await _repo.GetByIdAsync(query.Id, ct);
        return schedule is null ? null : ScheduleMapper.ToDto(schedule);
    }
}
