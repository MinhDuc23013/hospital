using DoctorScheduleService.Domain.Entities;
using DoctorScheduleService.Domain.Enums;
using HospitalShared.DTOs;

namespace DoctorScheduleService.Application.Handlers;

/// <summary>Converts domain entities to DTOs — shared by all handlers.</summary>
internal static class ScheduleMapper
{
    internal static DoctorScheduleDto ToDto(DoctorSchedule s) => new()
    {
        Id = s.Id,
        DoctorId = s.DoctorId,
        DoctorName = s.DoctorName,
        Date = s.Date,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        SlotDurationMinutes = s.SlotDurationMinutes,
        TotalSlots = s.Slots.Count,
        AvailableSlots = s.Slots.Count(sl => sl.Status == SlotStatus.Available),
        Status = s.Status.ToString(),
        CreatedAt = s.CreatedAt
    };

    internal static TimeSlotDto ToSlotDto(TimeSlot s) => new()
    {
        Id = s.Id,
        ScheduleId = s.ScheduleId,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        Status = s.Status.ToString(),
        AppointmentId = s.AppointmentId,
        PatientId = s.PatientId,
        ReservedUntil = s.ReservedUntil
    };
}
