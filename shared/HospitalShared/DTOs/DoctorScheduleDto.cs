namespace HospitalShared.DTOs;

/// <summary>Data transfer object for DoctorSchedule entity across services.</summary>
public class DoctorScheduleDto
{
    public Guid Id { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int SlotDurationMinutes { get; set; }
    public int TotalSlots { get; set; }
    public int AvailableSlots { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
}

/// <summary>Data transfer object for a single time slot within a doctor's schedule.</summary>
public class TimeSlotDto
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = "Available";
    public Guid? AppointmentId { get; set; }
    public Guid? PatientId { get; set; }
    public DateTime? ReservedUntil { get; set; }
}
