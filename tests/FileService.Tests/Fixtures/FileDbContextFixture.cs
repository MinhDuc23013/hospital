using FileService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FileService.Tests.Fixtures;

/// <summary>Fixture for setting up an in-memory FileDbContext for testing.</summary>
public sealed class FileDbContextFixture : IAsyncLifetime
{
    private readonly DbContextOptions<FileDbContext> _options;
    public FileDbContext Context { get; private set; } = null!;

    public FileDbContextFixture()
    {
        _options = new DbContextOptionsBuilder<FileDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Each test gets its own isolated DB
            .Options;
    }

    public async Task InitializeAsync()
    {
        Context = new FileDbContext(_options);
        // Ensure schema is created
        await Context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();
    }
}
