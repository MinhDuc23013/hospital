using DoctorScheduleService.Application.Queries;
using DoctorScheduleService.Infrastructure.Repositories;
using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Handlers;

public class ListDoctorsHandler : IRequestHandler<ListDoctorsQuery, (List<DoctorDto> Items, int Total)>
{
    private readonly IDoctorRepository _repo;
    public ListDoctorsHandler(IDoctorRepository repo) => _repo = repo;

    public async Task<(List<DoctorDto> Items, int Total)> Handle(ListDoctorsQuery query, CancellationToken ct)
    {
        var (doctors, total) = await _repo.ListAsync(query.Specialty, query.IsActive, query.Page, query.PageSize, ct);
        return (doctors.Select(DoctorMapper.ToDto).ToList(), total);
    }
}
