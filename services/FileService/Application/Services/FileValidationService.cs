using FileService.Application.Configuration;
using FileService.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace FileService.Application.Services;

/// <summary>Validates upload requests against size cap and content-type/extension allowlists.</summary>
public class FileValidationService
{
    private readonly FileStorageOptions _options;

    public FileValidationService(IOptions<FileStorageOptions> options) => _options = options.Value;

    public void Validate(string fileName, string contentType, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("File name is required.");

        if (sizeBytes <= 0)
            throw new DomainException("File is empty.");

        if (sizeBytes > _options.MaxSizeBytes)
            throw new DomainException($"File exceeds maximum allowed size of {_options.MaxSizeBytes} bytes.");

        if (!_options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new DomainException($"Content type '{contentType}' is not allowed.");

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new DomainException($"File extension '{extension}' is not allowed.");
    }

    /// <summary>Strips path separators/traversal segments so filename is safe to echo in headers/logs.</summary>
    public static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Replace('\\', '/'));
        return string.IsNullOrWhiteSpace(name) ? "file" : name;
    }
}
