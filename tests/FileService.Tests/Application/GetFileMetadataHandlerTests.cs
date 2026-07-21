using FileService.Application.Handlers;
using FileService.Application.Queries;
using FileService.Domain.Entities;
using FileService.Infrastructure.Repositories;
using FluentAssertions;
using HospitalShared;
using Moq;
using Xunit;

namespace FileService.Tests.Application;

public sealed class GetFileMetadataHandlerTests
{
    private readonly Mock<IFileRepository> _mockRepo = new();

    private GetFileMetadataHandler CreateHandler() => new(_mockRepo.Object);

    [Fact]
    public async Task Handle_FileExists_ReturnsFileMetadataDto()
    {
        var handler = CreateHandler();
        var fileId = GuidV7.NewGuid();
        var metadata = StoredFile.Create("document.pdf", "application/pdf", 1024, "user123", "abc123");

        var query = new GetFileMetadataQuery(fileId);

        _mockRepo
            .Setup(r => r.GetMetadataAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(metadata.Id);
        result.FileName.Should().Be("document.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.SizeBytes.Should().Be(1024);
        result.Sha256.Should().Be("abc123");
        result.UploadedBy.Should().Be("user123");
        result.UploadedAt.Should().Be(metadata.UploadedAt);

        _mockRepo.Verify(r => r.GetMetadataAsync(fileId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FileNotFound_ReturnsNull()
    {
        var handler = CreateHandler();
        var fileId = GuidV7.NewGuid();
        var query = new GetFileMetadataQuery(fileId);

        _mockRepo
            .Setup(r => r.GetMetadataAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredFile?)null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
        _mockRepo.Verify(r => r.GetMetadataAsync(fileId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        var handler = CreateHandler();
        var fileId = GuidV7.NewGuid();
        var cts = new CancellationTokenSource();
        var query = new GetFileMetadataQuery(fileId);

        _mockRepo
            .Setup(r => r.GetMetadataAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredFile?)null);

        await handler.Handle(query, cts.Token);

        _mockRepo.Verify(r => r.GetMetadataAsync(fileId, cts.Token), Times.Once);
    }
}
