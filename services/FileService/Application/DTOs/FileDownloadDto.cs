namespace FileService.Application.DTOs;

/// <summary>Download payload — includes raw content bytes for streaming back to client.</summary>
public class FileDownloadDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
