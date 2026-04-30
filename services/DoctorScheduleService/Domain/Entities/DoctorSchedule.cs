using HospitalShared;
using DoctorScheduleService.Domain.Enums;
using DoctorScheduleService.Domain.Exceptions;

namespace DoctorScheduleService.Domain.Entities;

/// <summary>Aggregate root representing a doctor's working schedule for a given date.</summary>
public class DoctorSchedule
{
    public Guid Id { get; private set; }
    public string DoctorId { get; private set; } = string.Empty;
    public string DoctorName { get; private set; } = string.Empty;
    public DateTime Date { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }
    public int SlotDurationMinutes { get; private set; }
    public ScheduleStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<TimeSlot> _slots = new();
    public IReadOnlyList<TimeSlot> Slots => _slots.AsReadOnly();

    private DoctorSchedule() { } // EF Core

    /// <summary>Creates a new schedule and auto-generates time slots.</summary>
    public static DoctorSchedule Create(
        string doctorId, string doctorName,
        DateTime date, TimeSpan startTime, TimeSpan endTime, int slotDurationMinutes)
    {
        var schedule = new DoctorSchedule
        {
            Id = GuidV7.NewGuid(),
            DoctorId = doctorId,
            DoctorName = doctorName,
            Date = date.Date,
            StartTime = startTime,
            EndTime = endTime,
            SlotDurationMinutes = slotDurationMinutes,
            Status = ScheduleStatus.Active,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        schedule.GenerateSlots();
        return schedule;
    }

    private void GenerateSlots()
    {
        var current = StartTime;
        while (current + TimeSpan.FromMinutes(SlotDurationMinutes) <= EndTime)
        {
            var slotEnd = current + TimeSpan.FromMinutes(SlotDurationMinutes);
            _slots.Add(TimeSlot.Create(Id, current, slotEnd));
            current = slotEnd;
        }
    }

    /// <summary>Marks a slot as Reserved and sets expiry. Throws if slot not found or unavailable.</summary>
    public TimeSlot ReserveSlot(Guid slotId, Guid patientId, int reservationMinutes = 15)
    {
        var slot = FindSlot(slotId);
        slot.Reserve(patientId, reservationMinutes);
        UpdatedAt = DateTime.Now;
        RefreshStatus();
        return slot;
    }

    /// <summary>Confirms a reserved slot by linking it to an appointment.</summary>
    public TimeSlot ConfirmSlot(Guid slotId, Guid appointmentId)
    {
        var slot = FindSlot(slotId);
        slot.Confirm(appointmentId);
        UpdatedAt = DateTime.Now;
        RefreshStatus();
        return slot;
    }

    /// <summary>Releases a slot back to Available.</summary>
    public TimeSlot ReleaseSlot(Guid slotId)
    {
        var slot = FindSlot(slotId);
        slot.Release();
        UpdatedAt = DateTime.Now;
        RefreshStatus();
        return slot;
    }

    /// <summary>Cancels a slot permanently.</summary>
    public TimeSlot CancelSlot(Guid slotId)
    {
        var slot = FindSlot(slotId);
        slot.Cancel();
        UpdatedAt = DateTime.Now;
        RefreshStatus();
        return slot;
    }

    private TimeSlot FindSlot(Guid slotId) =>
        _slots.FirstOrDefault(s => s.Id == slotId)
        ?? throw new NotFoundException("TimeSlot", slotId);

    private void RefreshStatus()
    {
        var hasAvailable = _slots.Any(s => s.Status == Enums.SlotStatus.Available);
        if (!hasAvailable && Status == ScheduleStatus.Active)
            Status = ScheduleStatus.Full;
        else if (hasAvailable && Status == ScheduleStatus.Full)
            Status = ScheduleStatus.Active;
    }
}
