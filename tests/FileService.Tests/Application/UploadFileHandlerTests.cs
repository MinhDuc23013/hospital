using FileService.Application.Commands;
using FileService.Application.Configuration;
using FileService.Application.DTOs;
using FileService.Application.Handlers;
using FileService.Application.Services;
using FileService.Domain.Exceptions;
using FileService.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FileService.Tests.Application;

public sealed class UploadFileHandlerTests
{
    private readonly Mock<IFileRepository> _mockRepo = new();
    private readonly FileStorageOptions _options = new()
    {
        MaxSizeBytes = 20 * 1024 * 1024,
        AllowedContentTypes = new[] { "application/pdf", "image/png", "image/jpeg", "text/plain" },
        AllowedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg", ".txt" }
    };

    private UploadFileHandler CreateHandler()
    {
        var optionsWrapper = Options.Create(_options);
        var validator = new FileValidationService(optionsWrapper);
        return new UploadFileHandler(_mockRepo.Object, validator);
    }

    [Fact]
    public async Task Handle_ValidFile_StoresAndReturnsMetadata()
    {
        var handler = CreateHandler();
        var content = new byte[] { 1, 2, 3, 4, 5 };
        var cmd = new UploadFileCommand(
            Content: content,
            FileName: "document.pdf",
            ContentType: "application/pdf",
            UploadedBy: "user123"
        );

        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<global::FileService.Domain.Entities.StoredFile>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().Be("document.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.SizeBytes.Should().Be(5);
        result.UploadedBy.Should().Be("user123");
        result.Sha256.Should().NotBeNullOrEmpty(); // SHA256 computed
        result.Id.Should().NotBeEmpty();

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<global::FileService.Domain.Entities.StoredFile>(), content, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OversizedFile_ThrowsDomainException()
    {
        var handler = CreateHandler();
        var oversizeContent = new byte[_options.MaxSizeBytes + 1];
        var cmd = new UploadFileCommand(
            Content: oversizeContent,
            FileName: "large.pdf",
            ContentType: "application/pdf",
            UploadedBy: "user123"
        );

        var action = () => handler.Handle(cmd, CancellationToken.None);
        await action.Should().ThrowAsync<DomainException>().WithMessage("*exceeds maximum allowed size*");

        // Verify repository was not called
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<global::FileService.Domain.Entities.StoredFile>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DisallowedContentType_ThrowsDomainException()
    {
        var handler = CreateHandler();
        var cmd = new UploadFileCommand(
            Content: new byte[] { 1, 2, 3 },
            FileName: "malware.exe",
            ContentType: "application/x-msdownload",
            UploadedBy: "user123"
        );

        var action = () => handler.Handle(cmd, CancellationToken.None);
        await action.Should().ThrowAsync<DomainException>().WithMessage("*not allowed*");

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<global::FileService.Domain.Entities.StoredFile>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DisallowedExtension_ThrowsDomainException()
    {
        var handler = CreateHandler();
        var cmd = new UploadFileCommand(
            Content: new byte[] { 1, 2, 3 },
            FileName: "file.exe",
            ContentType: "application/pdf",
            UploadedBy: "user123"
        );

        var action = () => handler.Handle(cmd, CancellationToken.None);
        await action.Should().ThrowAsync<DomainException>().WithMessage("*not allowed*");

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<global::FileService.Domain.Entities.StoredFile>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PathTraversalInFileName_Sanitized()
    {
        var handler = CreateHandler();
        var cmd = new UploadFileCommand(
            Content: new byte[] { 1, 2, 3 },
            FileName: "../../etc/passwd.txt",
            ContentType: "text/plain",
            UploadedBy: "user123"
        );

        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<global::FileService.Domain.Entities.StoredFile>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await handler.Handle(cmd, CancellationToken.None);

        // Verify filename is sanitized to just "passwd.txt"
        result.FileName.Should().Be("passwd.txt");
    }

    [Fact]
    public async Task Handle_ComputesSha256Hash()
    {
        var handler = CreateHandler();
        var content = new byte[] { 1, 2, 3, 4, 5 };
        var cmd = new UploadFileCommand(
            Content: content,
            FileName: "test.pdf",
            ContentType: "application/pdf",
            UploadedBy: "user123"
        );

        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<global::FileService.Domain.Entities.StoredFile>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await handler.Handle(cmd, CancellationToken.None);

        // SHA256 should be computed and lowercase
        result.Sha256.Should().NotBeNullOrEmpty();
        result.Sha256.Should().MatchRegex(@"^[0-9a-f]{64}$"); // 64 hex chars
    }

    [Fact]
    public async Task Handle_UploadedByCanBeNull()
    {
        var handler = CreateHandler();
        var cmd = new UploadFileCommand(
            Content: new byte[] { 1, 2, 3 },
            FileName: "document.pdf",
            ContentType: "application/pdf",
            UploadedBy: null
        );

        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<global::FileService.Domain.Entities.StoredFile>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.UploadedBy.Should().BeNull();
    }
}
