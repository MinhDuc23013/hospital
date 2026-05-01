using AppointmentService.Application.Queries;
using AppointmentService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;

namespace AppointmentService.Application.Handlers;

public class GetAppointmentHandler : IRequestHandler<GetAppointmentQuery, AppointmentDto?>
{
    private readonly IAppointmentReadRepository _repo;
    public GetAppointmentHandler(IAppointmentReadRepository repo) => _repo = repo;

    public async Task<AppointmentDto?> Handle(GetAppointmentQuery query, CancellationToken ct)
    {
        var appt = await _repo.GetByIdAsync(query.Id, ct);
        return appt is null ? null : ScheduleAppointmentHandler.MapToDto(appt);
    }
}
