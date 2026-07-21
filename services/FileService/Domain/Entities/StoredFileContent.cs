namespace FileService.Domain.Entities;

/// <summary>Binary content for a <see cref="StoredFile"/> — split into its own table (bytea only) to keep the metadata table free of TOAST bloat. Id is shared with the owning StoredFile (acts as FK).</summary>
public class StoredFileContent
{
    public Guid Id { get; private set; }
    public byte[] Content { get; private set; } = Array.Empty<byte>();

    private StoredFileContent() { } // EF Core constructor

    public static StoredFileContent Create(Guid id, byte[] content)
    {
        return new StoredFileContent
        {
            Id = id,
            Content = content
        };
    }

    /// <summary>Replaces the raw bytes — used when syncing edits back from Google Drive.</summary>
    public void ReplaceContent(byte[] content) => Content = content;
}
