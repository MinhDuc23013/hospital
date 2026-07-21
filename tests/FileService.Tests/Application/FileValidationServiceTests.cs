using FileService.Application.Configuration;
using FileService.Application.Services;
using FileService.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FileService.Tests.Application;

public sealed class FileValidationServiceTests
{
    private readonly FileStorageOptions _options = new()
    {
        MaxSizeBytes = 20 * 1024 * 1024, // 20 MB
        AllowedContentTypes = new[] { "application/pdf", "image/png", "image/jpeg", "text/plain", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        AllowedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg", ".txt", ".docx" }
    };

    private FileValidationService CreateValidator()
    {
        var optionsWrapper = Options.Create(_options);
        return new FileValidationService(optionsWrapper);
    }

    [Fact]
    public void Validate_ValidFile_Succeeds()
    {
        var validator = CreateValidator();
        var action = () => validator.Validate("document.pdf", "application/pdf", 1024);
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroByteFile_ThrowsDomainException()
    {
        var validator = CreateValidator();
        var action = () => validator.Validate("empty.pdf", "application/pdf", 0);
        action.Should().Throw<DomainException>().WithMessage("File is empty.");
    }

    [Fact]
    public void Validate_NegativeFileSize_ThrowsDomainException()
    {
        var validator = CreateValidator();
        var action = () => validator.Validate("file.pdf", "application/pdf", -1);
        action.Should().Throw<DomainException>().WithMessage("File is empty.");
    }

    [Fact]
    public void Validate_OversizedFile_ThrowsDomainException()
    {
        var validator = CreateValidator();
        var oversizeBytes = _options.MaxSizeBytes + 1;
        var action = () => validator.Validate("large.pdf", "application/pdf", oversizeBytes);
        action.Should().Throw<DomainException>().WithMessage($"File exceeds maximum allowed size of {_options.MaxSizeBytes} bytes.");
    }

    [Fact]
    public void Validate_ExactlyAtCapSize_Succeeds()
    {
        var validator = CreateValidator();
        var action = () => validator.Validate("file.pdf", "application/pdf", _options.MaxSizeBytes);
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_DisallowedContentType_ThrowsDomainException()
    {
        var validator = CreateValidator();
        var action = () => validator.Validate("file.pdf", "application/exe", 1024);
        action.Should().Throw<DomainException>().WithMessage("Content type 'application/exe' is not allowed.");
    }

    [Fact]
    public void Validate_ContentTypeCaseInsensitive_Succeeds()
    {
        var validator = CreateValidator();
        // Allow case-insensitive content type matching
        var action = () => validator.Validate("file.pdf", "APPLICATION/PDF", 1024);
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_DisallowedExtension_ThrowsDomainException()
    {
        var validator = CreateValidator();
        var action = () => validator.Validate("file.exe", "application/pdf", 1024);
        action.Should().Throw<DomainException>().WithMessage("File extension '.exe' is not allowed.");
    }

    [Fact]
    public void Validate_NoFileExtension_ThrowsDomainException()
    {
        var validator = CreateValidator();
        var action = () => validator.Validate("filename", "application/pdf", 1024);
        action.Should().Throw<DomainException>().WithMessage("File extension '' is not allowed.");
    }

    [Fact]
    public void Validate_ExtensionCaseInsensitive_Succeeds()
    {
        var validator = CreateValidator();
        var action = () => validator.Validate("file.PDF", "application/pdf", 1024);
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_EmptyFileName_ThrowsDomainException()
    {
        var validator = CreateValidator();
        var action = () => validator.Validate("", "application/pdf", 1024);
        action.Should().Throw<DomainException>().WithMessage("File name is required.");
    }

    [Fact]
    public void Validate_WhitespaceFileName_ThrowsDomainException()
    {
        var validator = CreateValidator();
        var action = () => validator.Validate("   ", "application/pdf", 1024);
        action.Should().Throw<DomainException>().WithMessage("File name is required.");
    }

    [Fact]
    public void Validate_NullFileName_ThrowsDomainException()
    {
        var validator = CreateValidator();
        var action = () => validator.Validate(null!, "application/pdf", 1024);
        action.Should().Throw<DomainException>().WithMessage("File name is required.");
    }

    [Fact]
    public void SanitizeFileName_RemovesPathTraversal()
    {
        var unsafeName = "../../etc/passwd";
        var result = FileValidationService.SanitizeFileName(unsafeName);
        result.Should().Be("passwd");
    }

    [Fact]
    public void SanitizeFileName_RemovesBackslashes()
    {
        var unsafeName = @"C:\Users\malicious\file.txt";
        var result = FileValidationService.SanitizeFileName(unsafeName);
        result.Should().Be("file.txt");
    }

    [Fact]
    public void SanitizeFileName_RemovesForwardSlashes()
    {
        var unsafeName = "/var/tmp/file.txt";
        var result = FileValidationService.SanitizeFileName(unsafeName);
        result.Should().Be("file.txt");
    }

    [Fact]
    public void SanitizeFileName_EmptyResult_ReturnsFallback()
    {
        var unsafeName = "///";
        var result = FileValidationService.SanitizeFileName(unsafeName);
        result.Should().Be("file");
    }

    [Fact]
    public void SanitizeFileName_NormalFileName_Unchanged()
    {
        var safe = "document.pdf";
        var result = FileValidationService.SanitizeFileName(safe);
        result.Should().Be("document.pdf");
    }
}
