namespace HospitalShared.DTOs;

/// <summary>Data transfer object for Patient entity across services.</summary>
public class PatientDto
{
    public Guid Id { get; set; }
    public string? KeycloakUserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
