using FileService.Application.Handlers;
using FileService.Application.Queries;
using FileService.Domain.Entities;
using FileService.Infrastructure.Repositories;
using FluentAssertions;
using HospitalShared;
using Moq;
using Xunit;

namespace FileService.Tests.Application;

public sealed class ListFilesHandlerTests
{
    private readonly Mock<IFileRepository> _mockRepo = new();

    private ListFilesHandler CreateHandler() => new(_mockRepo.Object);

    [Fact]
    public async Task Handle_ReturnsPagedList_WithMetadata()
    {
        var handler = CreateHandler();
        var file1 = StoredFile.Create("document1.pdf", "application/pdf", 1024, "user1", "sha1");
        var file2 = StoredFile.Create("document2.txt", "text/plain", 512, "user2", "sha2");

        var items = new List<StoredFile> { file1, file2 };

        var query = new ListFilesQuery(Page: 1, PageSize: 10);

        _mockRepo
            .Setup(r => r.ListAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 2)); // 2 total items

        var (resultItems, total) = await handler.Handle(query, CancellationToken.None);

        resultItems.Should().HaveCount(2);
        total.Should().Be(2);

        resultItems[0].Id.Should().Be(file1.Id);
        resultItems[0].FileName.Should().Be("document1.pdf");
        resultItems[1].Id.Should().Be(file2.Id);
        resultItems[1].FileName.Should().Be("document2.txt");

        _mockRepo.Verify(r => r.ListAsync(1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmptyWithZeroTotal()
    {
        var handler = CreateHandler();
        var query = new ListFilesQuery(Page: 1, PageSize: 10);

        _mockRepo
            .Setup(r => r.ListAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<StoredFile>(), 0));

        var (items, total) = await handler.Handle(query, CancellationToken.None);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task Handle_RespectsPagination()
    {
        var handler = CreateHandler();
        var file = StoredFile.Create("document.pdf", "application/pdf", 1024, "user1", "sha1");
        var items = new List<StoredFile> { file };

        var query = new ListFilesQuery(Page: 2, PageSize: 25);

        _mockRepo
            .Setup(r => r.ListAsync(2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 50)); // 50 total items, page 2 of 25

        var (resultItems, total) = await handler.Handle(query, CancellationToken.None);

        resultItems.Should().HaveCount(1);
        total.Should().Be(50);

        _mockRepo.Verify(r => r.ListAsync(2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        var handler = CreateHandler();
        var cts = new CancellationTokenSource();
        var query = new ListFilesQuery(Page: 1, PageSize: 10);

        _mockRepo
            .Setup(r => r.ListAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<StoredFile>(), 0));

        await handler.Handle(query, cts.Token);

        _mockRepo.Verify(r => r.ListAsync(1, 10, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsMetadataToDto_CorrectlyPreservesData()
    {
        var handler = CreateHandler();
        var file = StoredFile.Create("file.pdf", "application/pdf", 2048, "uploader", "hashvalue");
        var items = new List<StoredFile> { file };

        var query = new ListFilesQuery(Page: 1, PageSize: 10);

        _mockRepo
            .Setup(r => r.ListAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));

        var (resultItems, total) = await handler.Handle(query, CancellationToken.None);

        var item = resultItems[0];
        item.Id.Should().Be(file.Id);
        item.FileName.Should().Be("file.pdf");
        item.ContentType.Should().Be("application/pdf");
        item.SizeBytes.Should().Be(2048);
        item.Sha256.Should().Be("hashvalue");
        item.UploadedBy.Should().Be("uploader");
        item.UploadedAt.Should().Be(file.UploadedAt);
    }
}
