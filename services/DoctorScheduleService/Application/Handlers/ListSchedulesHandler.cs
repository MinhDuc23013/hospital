using DoctorScheduleService.Application.Queries;
using DoctorScheduleService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class ListSchedulesHandler : IRequestHandler<ListSchedulesQuery, (List<DoctorScheduleDto> Items, int Total)>
{
    private readonly IDoctorScheduleRepository _repo;

    public ListSchedulesHandler(IDoctorScheduleRepository repo) => _repo = repo;

    public async Task<(List<DoctorScheduleDto> Items, int Total)> Handle(
        ListSchedulesQuery query, CancellationToken ct)
    {
        var (schedules, total) = await _repo.ListAsync(
            query.DoctorId, query.Date, query.Page, query.PageSize, ct);
        return (schedules.Select(ScheduleMapper.ToDto).ToList(), total);
    }
}
