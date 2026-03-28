namespace HospitalShared.Events;

/// <summary>Published when a new patient profile is created.</summary>
public class PatientCreatedEvent
{
    public Guid PatientId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
