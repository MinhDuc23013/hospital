using DoctorScheduleService.Application.Queries;
using DoctorScheduleService.Domain.Exceptions;
using DoctorScheduleService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class GetAllSlotsHandler : IRequestHandler<GetAllSlotsQuery, List<TimeSlotDto>>
{
    private readonly IDoctorScheduleRepository _repo;

    public GetAllSlotsHandler(IDoctorScheduleRepository repo) => _repo = repo;

    public async Task<List<TimeSlotDto>> Handle(GetAllSlotsQuery query, CancellationToken ct)
    {
        var schedule = await _repo.GetByIdAsync(query.ScheduleId, ct)
            ?? throw new NotFoundException("DoctorSchedule", query.ScheduleId);

        return schedule.Slots
            .OrderBy(s => s.StartTime)
            .Select(ScheduleMapper.ToSlotDto)
            .ToList();
    }
}
