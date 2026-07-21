namespace FileService.Application.Services;

/// <summary>Parsed fields from Google's downloaded OAuth client-secret JSON ("web" section).</summary>
public record GoogleOAuthClientInfo(
    string ClientId,
    string ClientSecret,
    string RedirectUri,
    string AuthUri,
    string TokenUri);
