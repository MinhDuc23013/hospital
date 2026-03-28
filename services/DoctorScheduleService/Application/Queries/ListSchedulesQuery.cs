using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Queries;

public record ListSchedulesQuery(
    string? DoctorId,
    DateTime? Date,
    int Page = 1,
    int PageSize = 50
) : IRequest<(List<DoctorScheduleDto> Items, int Total)>;
