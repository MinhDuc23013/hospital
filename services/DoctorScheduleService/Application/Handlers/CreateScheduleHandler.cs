using DoctorScheduleService.Application.Commands;
using DoctorScheduleService.Domain.Entities;
using DoctorScheduleService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class CreateScheduleHandler : IRequestHandler<CreateScheduleCommand, DoctorScheduleDto>
{
    private readonly IDoctorScheduleRepository _repo;

    public CreateScheduleHandler(IDoctorScheduleRepository repo) => _repo = repo;

    public async Task<DoctorScheduleDto> Handle(CreateScheduleCommand cmd, CancellationToken ct)
    {
        var schedule = DoctorSchedule.Create(
            cmd.DoctorId, cmd.DoctorName,
            cmd.Date, cmd.StartTime, cmd.EndTime, cmd.SlotDurationMinutes);

        await _repo.AddAsync(schedule, ct);
        await _repo.SaveChangesAsync(ct);

        return ScheduleMapper.ToDto(schedule);
    }
}
