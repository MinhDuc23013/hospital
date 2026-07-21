namespace FileService.Application.Configuration;

/// <summary>Bound from "GoogleDrive" appsettings section.</summary>
public class GoogleDriveOptions
{
    public const string SectionName = "GoogleDrive";

    public string FolderId { get; set; } = string.Empty;
}
