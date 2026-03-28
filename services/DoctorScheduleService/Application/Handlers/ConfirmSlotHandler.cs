using DoctorScheduleService.Application.Commands;
using DoctorScheduleService.Domain.Exceptions;
using DoctorScheduleService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class ConfirmSlotHandler : IRequestHandler<ConfirmSlotCommand, TimeSlotDto>
{
    private readonly IDoctorScheduleRepository _repo;

    public ConfirmSlotHandler(IDoctorScheduleRepository repo) => _repo = repo;

    public async Task<TimeSlotDto> Handle(ConfirmSlotCommand cmd, CancellationToken ct)
    {
        var schedule = await _repo.GetByIdAsync(cmd.ScheduleId, ct)
            ?? throw new NotFoundException("DoctorSchedule", cmd.ScheduleId);

        var slot = schedule.ConfirmSlot(cmd.SlotId, cmd.AppointmentId);
        await _repo.SaveChangesAsync(ct);

        return ScheduleMapper.ToSlotDto(slot);
    }
}
