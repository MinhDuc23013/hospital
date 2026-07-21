using FileService.Domain.Entities;
using FileService.Infrastructure.Repositories;
using FileService.Tests.Fixtures;
using FluentAssertions;
using HospitalShared;
using Xunit;

namespace FileService.Tests.Infrastructure;

public sealed class FileRepositoryTests : IAsyncLifetime
{
    private readonly FileDbContextFixture _fixture = new();
    private IFileRepository _repo = null!;

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _repo = new FileRepository(_fixture.Context);
    }

    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    [Fact]
    public async Task AddAsync_StoresMetadataAndContentInDatabase()
    {
        var content = new byte[] { 1, 2, 3, 4, 5 };
        var file = StoredFile.Create("test.pdf", "application/pdf", content.LongLength, "user1", "sha1");

        await _repo.AddAsync(file, content, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var result = await _repo.GetWithContentAsync(file.Id, CancellationToken.None);
        result.Should().NotBeNull();
        result!.Value.Metadata.FileName.Should().Be("test.pdf");
        result.Value.Metadata.ContentType.Should().Be("application/pdf");
        result.Value.Content.Should().Equal(content);
    }

    [Fact]
    public async Task GetWithContentAsync_ReturnsFileWithContent()
    {
        var content = new byte[] { 1, 2, 3 };
        var file = StoredFile.Create("document.pdf", "application/pdf", content.LongLength, "user1", "hash");
        await _repo.AddAsync(file, content, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var result = await _repo.GetWithContentAsync(file.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Value.Metadata.Id.Should().Be(file.Id);
        result.Value.Metadata.FileName.Should().Be("document.pdf");
        result.Value.Metadata.ContentType.Should().Be("application/pdf");
        result.Value.Content.Should().Equal(content);
        result.Value.Metadata.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetWithContentAsync_ReturnsNull_WhenFileNotFound()
    {
        var nonExistentId = GuidV7.NewGuid();
        var result = await _repo.GetWithContentAsync(nonExistentId, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWithContentAsync_IgnoresInactiveFiles()
    {
        var content = new byte[] { 1, 2, 3 };
        var file = StoredFile.Create("document.pdf", "application/pdf", content.LongLength, "user1", "hash");
        await _repo.AddAsync(file, content, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        // Deactivate the file
        file.Deactivate();
        await _repo.SaveChangesAsync(CancellationToken.None);

        var result = await _repo.GetWithContentAsync(file.Id, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMetadataAsync_ReturnsMetadataWithoutContent()
    {
        var content = new byte[] { 1, 2, 3, 4, 5 };
        var file = StoredFile.Create("document.pdf", "application/pdf", content.LongLength, "user1", "hash123");
        await _repo.AddAsync(file, content, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var metadata = await _repo.GetMetadataAsync(file.Id, CancellationToken.None);

        metadata.Should().NotBeNull();
        metadata!.Id.Should().Be(file.Id);
        metadata.FileName.Should().Be("document.pdf");
        metadata.ContentType.Should().Be("application/pdf");
        metadata.SizeBytes.Should().Be(5);
        metadata.Sha256.Should().Be("hash123");
        metadata.UploadedBy.Should().Be("user1");
    }

    [Fact]
    public async Task GetMetadataAsync_ReturnsNull_WhenFileNotFound()
    {
        var nonExistentId = GuidV7.NewGuid();
        var metadata = await _repo.GetMetadataAsync(nonExistentId, CancellationToken.None);
        metadata.Should().BeNull();
    }

    [Fact]
    public async Task GetMetadataAsync_IgnoresInactiveFiles()
    {
        var content = new byte[] { 1, 2, 3 };
        var file = StoredFile.Create("document.pdf", "application/pdf", content.LongLength, "user1", "hash");
        await _repo.AddAsync(file, content, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        file.Deactivate();
        await _repo.SaveChangesAsync(CancellationToken.None);

        var metadata = await _repo.GetMetadataAsync(file.Id, CancellationToken.None);
        metadata.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_ReturnsPaginatedMetadata()
    {
        // Add 3 files
        for (int i = 0; i < 3; i++)
        {
            var file = StoredFile.Create($"file{i}.pdf", "application/pdf", 100, $"user{i}", $"hash{i}");
            await _repo.AddAsync(file, new byte[100], CancellationToken.None);
        }
        await _repo.SaveChangesAsync(CancellationToken.None);

        var (items, total) = await _repo.ListAsync(page: 1, pageSize: 2, CancellationToken.None);

        items.Should().HaveCount(2);
        total.Should().Be(3);
    }

    [Fact]
    public async Task ListAsync_RespectsPagination()
    {
        // Add 5 files
        var fileIds = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            var file = StoredFile.Create($"file{i}.pdf", "application/pdf", 100, $"user{i}", $"hash{i}");
            await _repo.AddAsync(file, new byte[100], CancellationToken.None);
            fileIds.Add(file.Id);
        }
        await _repo.SaveChangesAsync(CancellationToken.None);

        var page1 = await _repo.ListAsync(page: 1, pageSize: 2, CancellationToken.None);
        var page2 = await _repo.ListAsync(page: 2, pageSize: 2, CancellationToken.None);
        var page3 = await _repo.ListAsync(page: 3, pageSize: 2, CancellationToken.None);

        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(2);
        page3.Items.Should().HaveCount(1);
        page1.Total.Should().Be(5);
        page2.Total.Should().Be(5);
        page3.Total.Should().Be(5);
    }

    [Fact]
    public async Task ListAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var (items, total) = await _repo.ListAsync(page: 1, pageSize: 10, CancellationToken.None);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task ListAsync_IgnoresInactiveFiles()
    {
        var file1 = StoredFile.Create("file1.pdf", "application/pdf", 100, "user1", "hash1");
        var file2 = StoredFile.Create("file2.pdf", "application/pdf", 100, "user2", "hash2");
        var file3 = StoredFile.Create("file3.pdf", "application/pdf", 100, "user3", "hash3");

        await _repo.AddAsync(file1, new byte[100], CancellationToken.None);
        await _repo.AddAsync(file2, new byte[100], CancellationToken.None);
        await _repo.AddAsync(file3, new byte[100], CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        // Deactivate one file
        file2.Deactivate();
        await _repo.SaveChangesAsync(CancellationToken.None);

        var (items, total) = await _repo.ListAsync(page: 1, pageSize: 10, CancellationToken.None);

        items.Should().HaveCount(2);
        total.Should().Be(2);
        items.Should().NotContain(m => m.Id == file2.Id);
    }

    [Fact]
    public async Task ListAsync_SortsByUploadedAtDescending()
    {
        var file1 = StoredFile.Create("file1.pdf", "application/pdf", 100, "user1", "hash1");
        var file2 = StoredFile.Create("file2.pdf", "application/pdf", 100, "user2", "hash2");

        await _repo.AddAsync(file1, new byte[100], CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        // Wait a bit to ensure different timestamps
        await Task.Delay(10);

        await _repo.AddAsync(file2, new byte[100], CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var (items, _) = await _repo.ListAsync(page: 1, pageSize: 10, CancellationToken.None);

        // Most recent (file2) should come first
        items[0].Id.Should().Be(file2.Id);
        items[1].Id.Should().Be(file1.Id);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsChanges()
    {
        var file = StoredFile.Create("test.pdf", "application/pdf", 100, "user1", "hash1");
        await _repo.AddAsync(file, new byte[100], CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        // Query via the DbContext directly to verify persistence
        var directQuery = _fixture.Context.Files.FirstOrDefault(f => f.Id == file.Id);
        directQuery.Should().NotBeNull();
        directQuery!.FileName.Should().Be("test.pdf");
    }

    [Fact]
    public async Task Deactivate_SoftDelete_IsActive_SetToFalse()
    {
        var content = new byte[] { 1, 2, 3 };
        var file = StoredFile.Create("document.pdf", "application/pdf", content.LongLength, "user1", "hash");
        await _repo.AddAsync(file, content, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        // File should be active initially
        file.IsActive.Should().BeTrue();

        file.Deactivate();
        await _repo.SaveChangesAsync(CancellationToken.None);

        // Verify the in-memory state was updated
        file.IsActive.Should().BeFalse();

        // Verify it's not returned by normal queries (soft delete works)
        var result = await _repo.GetWithContentAsync(file.Id, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Deactivate_SoftDelete_DoesNotRemoveContentRow()
    {
        // Soft delete only flips IsActive on stored_files — the content row in
        // stored_file_contents is untouched (no hard delete/cascade triggered).
        var content = new byte[] { 9, 8, 7 };
        var file = StoredFile.Create("document.pdf", "application/pdf", content.LongLength, "user1", "hash");
        await _repo.AddAsync(file, content, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        file.Deactivate();
        await _repo.SaveChangesAsync(CancellationToken.None);

        var contentRow = _fixture.Context.Contents.FirstOrDefault(c => c.Id == file.Id);
        contentRow.Should().NotBeNull();
        contentRow!.Content.Should().Equal(content);
    }
}
