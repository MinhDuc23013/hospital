using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Queries;

/// <summary>Return ALL slots regardless of status (Available, Reserved, Confirmed).</summary>
public record GetAllSlotsQuery(Guid ScheduleId) : IRequest<List<TimeSlotDto>>;
