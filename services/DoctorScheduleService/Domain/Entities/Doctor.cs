namespace DoctorScheduleService.Domain.Entities;

/// <summary>Doctor entity — represents a healthcare provider.</summary>
public class Doctor
{
    public Guid Id { get; private set; }
    public string? KeycloakUserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Specialty { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Doctor() { } // EF Core

    public void LinkKeycloakUser(string keycloakUserId)
    {
        KeycloakUserId = keycloakUserId;
        UpdatedAt = DateTime.Now;
    }

    public static Doctor Create(string fullName, string specialty, string? phone = null, string? email = null, string? keycloakUserId = null)
    {
        return new Doctor
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Specialty = specialty,
            Phone = phone,
            Email = email,
            KeycloakUserId = keycloakUserId,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    public void Update(string fullName, string specialty, string? phone, string? email)
    {
        FullName = fullName;
        Specialty = specialty;
        Phone = phone;
        Email = email;
        UpdatedAt = DateTime.Now;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.Now; }
    public void Activate() { IsActive = true; UpdatedAt = DateTime.Now; }
}
