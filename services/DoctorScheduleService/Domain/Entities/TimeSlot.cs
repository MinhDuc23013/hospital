using HospitalShared;
using DoctorScheduleService.Domain.Enums;

namespace DoctorScheduleService.Domain.Entities;

/// <summary>Represents a single bookable time slot within a doctor's schedule.</summary>
public class TimeSlot
{
    public Guid Id { get; private set; }
    public Guid ScheduleId { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }
    public SlotStatus Status { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public Guid? PatientId { get; private set; }
    public DateTime? ReservedUntil { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TimeSlot() { } // EF Core

    internal static TimeSlot Create(Guid scheduleId, TimeSpan startTime, TimeSpan endTime) => new()
    {
        Id = GuidV7.NewGuid(),
        ScheduleId = scheduleId,
        StartTime = startTime,
        EndTime = endTime,
        Status = SlotStatus.Available,
        CreatedAt = DateTime.Now
    };

    internal void Reserve(Guid patientId, int reservationMinutes)
    {
        if (Status != SlotStatus.Available)
            throw new Exceptions.DomainException($"Slot {Id} is not available (current status: {Status}).");
        Status = SlotStatus.Reserved;
        PatientId = patientId;
        ReservedUntil = DateTime.Now.AddMinutes(reservationMinutes);
    }

    internal void Confirm(Guid appointmentId)
    {
        if (Status != SlotStatus.Reserved)
            throw new Exceptions.DomainException($"Slot {Id} must be Reserved before confirming (current: {Status}).");
        Status = SlotStatus.Booked;
        AppointmentId = appointmentId;
        ReservedUntil = null;
    }

    internal void Release()
    {
        Status = SlotStatus.Available;
        PatientId = null;
        AppointmentId = null;
        ReservedUntil = null;
    }

    internal void Cancel()
    {
        Status = SlotStatus.Cancelled;
    }
}
