namespace FileService.Application.Configuration;

/// <summary>Bound from "FileStorage" appsettings section.</summary>
public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public long MaxSizeBytes { get; set; } = 20 * 1024 * 1024;
    public string[] AllowedContentTypes { get; set; } = Array.Empty<string>();
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
}
