using DoctorScheduleService.Application.Queries;
using DoctorScheduleService.Domain.Enums;
using DoctorScheduleService.Domain.Exceptions;
using DoctorScheduleService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class GetAvailableSlotsHandler : IRequestHandler<GetAvailableSlotsQuery, List<TimeSlotDto>>
{
    private readonly IDoctorScheduleRepository _repo;

    public GetAvailableSlotsHandler(IDoctorScheduleRepository repo) => _repo = repo;

    public async Task<List<TimeSlotDto>> Handle(GetAvailableSlotsQuery query, CancellationToken ct)
    {
        var schedule = await _repo.GetByIdAsync(query.ScheduleId, ct)
            ?? throw new NotFoundException("DoctorSchedule", query.ScheduleId);

        return schedule.Slots
            .Where(s => s.Status == SlotStatus.Available)
            .Select(ScheduleMapper.ToSlotDto)
            .ToList();
    }
}
