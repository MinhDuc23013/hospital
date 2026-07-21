namespace FileService.Application.Configuration;

/// <summary>Bound from "GoogleOAuth" appsettings section.</summary>
public class GoogleOAuthOptions
{
    public const string SectionName = "GoogleOAuth";

    public string ClientSecretJsonPath { get; set; } = string.Empty;
}
