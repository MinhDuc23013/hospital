using FileService.Application.DTOs;
using FileService.Application.Handlers;
using FileService.Application.Queries;
using FileService.Domain.Entities;
using FileService.Infrastructure.Repositories;
using FluentAssertions;
using HospitalShared;
using Moq;
using Xunit;

namespace FileService.Tests.Application;

public sealed class GetFileHandlerTests
{
    private readonly Mock<IFileRepository> _mockRepo = new();

    private GetFileHandler CreateHandler() => new(_mockRepo.Object);

    [Fact]
    public async Task Handle_FileExists_ReturnsFileDownloadDto()
    {
        var handler = CreateHandler();
        var fileId = GuidV7.NewGuid();
        var content = new byte[] { 1, 2, 3, 4, 5 };
        var file = StoredFile.Create("document.pdf", "application/pdf", content.LongLength, "user123", "abc123");

        var query = new GetFileQuery(fileId);

        _mockRepo
            .Setup(r => r.GetWithContentAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((file, content));

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.FileName.Should().Be("document.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.Content.Should().Equal(content);

        _mockRepo.Verify(r => r.GetWithContentAsync(fileId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FileNotFound_ReturnsNull()
    {
        var handler = CreateHandler();
        var fileId = GuidV7.NewGuid();
        var query = new GetFileQuery(fileId);

        _mockRepo
            .Setup(r => r.GetWithContentAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((StoredFile, byte[])?)null);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
        _mockRepo.Verify(r => r.GetWithContentAsync(fileId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        var handler = CreateHandler();
        var fileId = GuidV7.NewGuid();
        var cts = new CancellationTokenSource();
        var query = new GetFileQuery(fileId);

        _mockRepo
            .Setup(r => r.GetWithContentAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((StoredFile, byte[])?)null);

        await handler.Handle(query, cts.Token);

        _mockRepo.Verify(r => r.GetWithContentAsync(fileId, cts.Token), Times.Once);
    }
}
