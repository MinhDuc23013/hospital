using DoctorScheduleService.Application.Commands;
using DoctorScheduleService.Domain.Exceptions;
using DoctorScheduleService.Infrastructure.MessageBus;
using DoctorScheduleService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using HospitalShared.Events;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class ReserveSlotHandler : IRequestHandler<ReserveSlotCommand, TimeSlotDto>
{
    private readonly IDoctorScheduleRepository _repo;
    private readonly EventPublisher _events;

    public ReserveSlotHandler(IDoctorScheduleRepository repo, EventPublisher events)
    {
        _repo = repo;
        _events = events;
    }

    public async Task<TimeSlotDto> Handle(ReserveSlotCommand cmd, CancellationToken ct)
    {
        var schedule = await _repo.GetByIdAsync(cmd.ScheduleId, ct)
            ?? throw new NotFoundException("DoctorSchedule", cmd.ScheduleId);

        var slot = schedule.ReserveSlot(cmd.SlotId, cmd.PatientId);
        await _repo.SaveChangesAsync(ct);

        await _events.PublishAsync(new SlotReservedEvent
        {
            SlotId = slot.Id,
            ScheduleId = schedule.Id,
            DoctorId = schedule.DoctorId,
            PatientId = cmd.PatientId,
            ScheduledTime = schedule.Date.Add(slot.StartTime),
            DurationMinutes = schedule.SlotDurationMinutes,
            ReservedUntil = slot.ReservedUntil!.Value
        }, ct);

        return ScheduleMapper.ToSlotDto(slot);
    }
}
