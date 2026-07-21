using FileService.Application.Commands;
using FileService.Application.Handlers;
using FileService.Domain.Entities;
using FileService.Domain.Exceptions;
using FileService.Infrastructure.Repositories;
using FluentAssertions;
using HospitalShared;
using Moq;
using Xunit;

namespace FileService.Tests.Application;

public sealed class DeleteFileHandlerTests
{
    private readonly Mock<IFileRepository> _mockRepo = new();

    private DeleteFileHandler CreateHandler() => new(_mockRepo.Object);

    [Fact]
    public async Task Handle_FileExists_DeactivatesAndSavesChanges()
    {
        var handler = CreateHandler();
        var fileId = GuidV7.NewGuid();
        var file = StoredFile.Create("document.pdf", "application/pdf", 3, "user123", "sha1");

        var cmd = new DeleteFileCommand(fileId);

        _mockRepo
            .Setup(r => r.GetMetadataAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _mockRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await handler.Handle(cmd, CancellationToken.None);

        // Verify file was deactivated
        file.IsActive.Should().BeFalse();

        // Verify SaveChangesAsync was called
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.GetMetadataAsync(fileId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FileNotFound_ThrowsNotFoundException()
    {
        var handler = CreateHandler();
        var fileId = GuidV7.NewGuid();
        var cmd = new DeleteFileCommand(fileId);

        _mockRepo
            .Setup(r => r.GetMetadataAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredFile?)null);

        var action = () => handler.Handle(cmd, CancellationToken.None);
        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*StoredFile*{fileId}*not found*");

        // Verify SaveChangesAsync was NOT called
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        var handler = CreateHandler();
        var fileId = GuidV7.NewGuid();
        var cts = new CancellationTokenSource();
        var file = StoredFile.Create("document.pdf", "application/pdf", 3, "user123", "sha1");

        var cmd = new DeleteFileCommand(fileId);

        _mockRepo
            .Setup(r => r.GetMetadataAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _mockRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await handler.Handle(cmd, cts.Token);

        // Verify both calls used the provided cancellation token
        _mockRepo.Verify(r => r.GetMetadataAsync(fileId, cts.Token), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_SetIsActiveFalse_PreservesOtherProperties()
    {
        var handler = CreateHandler();
        var fileId = GuidV7.NewGuid();
        var file = StoredFile.Create("document.pdf", "application/pdf", 5, "user123", "abcdef");

        var cmd = new DeleteFileCommand(fileId);

        _mockRepo
            .Setup(r => r.GetMetadataAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _mockRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await handler.Handle(cmd, CancellationToken.None);

        // Verify only IsActive changed
        file.IsActive.Should().BeFalse();
        file.Id.Should().Be(file.Id);
        file.FileName.Should().Be("document.pdf");
        file.ContentType.Should().Be("application/pdf");
        file.SizeBytes.Should().Be(5);
        file.UploadedBy.Should().Be("user123");
    }
}
