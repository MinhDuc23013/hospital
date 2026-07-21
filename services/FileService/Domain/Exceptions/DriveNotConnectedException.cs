namespace FileService.Domain.Exceptions;

/// <summary>Thrown when the Google Drive "edit" feature is used before the org-wide OAuth connection is set up.</summary>
public class DriveNotConnectedException : DomainException
{
    public DriveNotConnectedException()
        : base("Google Drive is not connected. Connect it first via /api/files/google/auth-url.") { }
}
