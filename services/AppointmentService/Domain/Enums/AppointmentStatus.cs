namespace AppointmentService.Domain.Enums;

public enum AppointmentStatus
{
    Scheduled = 0,
    Confirmed = 1,   // async phase confirmed slot
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    NoShow = 5
}
