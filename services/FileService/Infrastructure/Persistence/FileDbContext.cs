using FileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileService.Infrastructure.Persistence;

public class FileDbContext : DbContext
{
    public DbSet<StoredFile> Files => Set<StoredFile>();
    public DbSet<StoredFileContent> Contents => Set<StoredFileContent>();

    public FileDbContext(DbContextOptions<FileDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new StoredFileConfiguration());
        modelBuilder.ApplyConfiguration(new StoredFileContentConfiguration());

        modelBuilder.Entity<StoredFile>()
            .HasOne<StoredFileContent>()
            .WithOne()
            .HasForeignKey<StoredFileContent>(c => c.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
