using DoctorScheduleService.Application.Commands;
using DoctorScheduleService.Domain.Exceptions;
using DoctorScheduleService.Infrastructure.Repositories;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class ReleaseSlotHandler : IRequestHandler<ReleaseSlotCommand, bool>
{
    private readonly IDoctorScheduleRepository _repo;

    public ReleaseSlotHandler(IDoctorScheduleRepository repo) => _repo = repo;

    public async Task<bool> Handle(ReleaseSlotCommand cmd, CancellationToken ct)
    {
        var schedule = await _repo.GetByIdAsync(cmd.ScheduleId, ct)
            ?? throw new NotFoundException("DoctorSchedule", cmd.ScheduleId);

        schedule.ReleaseSlot(cmd.SlotId);
        await _repo.SaveChangesAsync(ct);
        return true;
    }
}
