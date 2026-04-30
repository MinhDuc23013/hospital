using HospitalShared;
namespace PatientService.Domain.Entities;

/// <summary>Patient aggregate root — core medical identity in the system.</summary>
public class Patient
{
    public Guid Id { get; private set; }
    public string? KeycloakUserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateTime DateOfBirth { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Patient() { } // EF Core constructor

    public void LinkKeycloakUser(string keycloakUserId)
    {
        KeycloakUserId = keycloakUserId;
        UpdatedAt = DateTime.Now;
    }

    public static Patient Create(string email, string firstName, string lastName, DateTime dateOfBirth, string? phone = null, string? keycloakUserId = null)
    {
        return new Patient
        {
            Id = GuidV7.NewGuid(),
            Email = email.ToLowerInvariant().Trim(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DateOfBirth = dateOfBirth,
            PhoneNumber = phone,
            KeycloakUserId = keycloakUserId,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    public void Update(string firstName, string lastName, string? phoneNumber)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber;
        UpdatedAt = DateTime.Now;
    }

    public void Deactivate() => IsActive = false;
}
