using FileService.Domain.Entities;

namespace FileService.Infrastructure.Repositories;

public interface IGoogleDriveConnectionRepository
{
    /// <summary>There's at most one connection row ever — returns it, or null if never connected.</summary>
    Task<GoogleDriveConnection?> GetAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the single connection row in place if it exists, else inserts a new one.
    /// RefreshToken/ConnectedEmail are only overwritten when the new value is non-empty
    /// (Google may omit refresh_token on non-first consents).
    /// </summary>
    Task UpsertAsync(string accessToken, string refreshToken, DateTime expiresAt, string connectedEmail, CancellationToken ct = default);

    /// <summary>Used by the token-refresh path. Throws if no connection row exists.</summary>
    Task UpdateAccessTokenAsync(string accessToken, DateTime expiresAt, CancellationToken ct = default);
}
