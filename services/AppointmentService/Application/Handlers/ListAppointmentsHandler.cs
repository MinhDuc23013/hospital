using AppointmentService.Application.Queries;
using AppointmentService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;

namespace AppointmentService.Application.Handlers;

public class ListAppointmentsHandler : IRequestHandler<ListAppointmentsQuery, (List<AppointmentDto> Items, int Total)>
{
    private readonly IAppointmentReadRepository _repo;
    public ListAppointmentsHandler(IAppointmentReadRepository repo) => _repo = repo;

    public async Task<(List<AppointmentDto> Items, int Total)> Handle(ListAppointmentsQuery query, CancellationToken ct)
    {
        var (appointments, total) = await _repo.ListAsync(
            query.PatientId, query.DoctorId, query.Page, query.PageSize, ct);
        return (appointments.Select(ScheduleAppointmentHandler.MapToDto).ToList(), total);
    }
}
