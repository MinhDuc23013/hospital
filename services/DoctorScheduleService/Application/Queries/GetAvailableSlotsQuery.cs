using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Queries;

public record GetAvailableSlotsQuery(Guid ScheduleId) : IRequest<List<TimeSlotDto>>;
