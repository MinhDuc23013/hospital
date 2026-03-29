using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Queries;

public record ListDoctorsQuery(
    string? Specialty,
    bool? IsActive,
    int Page = 1,
    int PageSize = 50
) : IRequest<(List<DoctorDto> Items, int Total)>;
