using HospitalShared;

namespace FileService.Domain.Entities;

/// <summary>
/// Single-row org-wide OAuth connection state for the Google Drive "edit" feature.
/// A real Google user authorizes this app once; that user's Drive quota is used
/// for all future uploads (no per-employee auth wired up — accepted simplification).
/// </summary>
public class GoogleDriveConnection
{
    public Guid Id { get; private set; }
    public string AccessToken { get; private set; } = string.Empty;
    public string RefreshToken { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public string ConnectedEmail { get; private set; } = string.Empty;
    public DateTime ConnectedAt { get; private set; }

    private GoogleDriveConnection() { } // EF Core constructor

    public static GoogleDriveConnection Create(
        string accessToken, string refreshToken, DateTime expiresAt, string connectedEmail)
    {
        return new GoogleDriveConnection
        {
            Id = GuidV7.NewGuid(),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            ConnectedEmail = connectedEmail,
            ConnectedAt = DateTime.UtcNow
        };
    }

    /// <summary>Refresh-in-place: only the access token/expiry change, refresh token stays.</summary>
    public void UpdateToken(string accessToken, DateTime expiresAt)
    {
        AccessToken = accessToken;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Re-consent upsert: access token/expiry always updated; refresh token/email only
    /// overwritten when Google actually returned a new value.
    /// </summary>
    public void ReconsentUpdate(string accessToken, DateTime expiresAt, string? refreshToken, string? connectedEmail)
    {
        AccessToken = accessToken;
        ExpiresAt = expiresAt;
        if (!string.IsNullOrWhiteSpace(refreshToken))
            RefreshToken = refreshToken;
        if (!string.IsNullOrWhiteSpace(connectedEmail))
            ConnectedEmail = connectedEmail;
    }
}
