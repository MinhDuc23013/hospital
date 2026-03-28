using HospitalShared.DTOs;
using MediatR;

namespace DoctorScheduleService.Application.Commands;

public record CreateScheduleCommand(
    string DoctorId,
    string DoctorName,
    DateTime Date,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int SlotDurationMinutes
) : IRequest<DoctorScheduleDto>;
