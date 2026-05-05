namespace HospitalGateway.Bff;

public record BffSession(
    string AccessToken,
    string RefreshToken,
    long ExpiresAt,
    string UserId,
    string Email,
    string? FullName,
    string[] Roles
);
