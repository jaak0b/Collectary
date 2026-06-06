namespace Collectary.Core.Ports;

public record CloudFile(string Id, string Name, long Size);

public record CloudFolder(string Id, string Name);

/// <summary>
/// Minimal folder/file CRUD against a remote cloud root (OneDrive, Google Drive, …).
/// Providers stay thin: the {kind}/{id}.{rev}.json sync layout is layered on top by
/// <c>CloudSyncBackend</c>, so each provider only implements flat folder operations.
/// </summary>
public interface ICloudFileStore
{
    bool IsAvailable { get; }
    string RootFolderId { get; }

    /// <summary>Returns the id of the child folder named <paramref name="name"/>, creating it if absent.</summary>
    Task<string> EnsureFolderAsync(string parentFolderId, string name, CancellationToken ct);

    Task<IReadOnlyList<CloudFile>> ListFilesAsync(string folderId, CancellationToken ct);
    Task<IReadOnlyList<CloudFolder>> ListFoldersAsync(string folderId, CancellationToken ct);

    Task<byte[]?> DownloadAsync(string folderId, string name, CancellationToken ct);
    Task UploadAsync(string folderId, string name, byte[] content, CancellationToken ct);
    Task DeleteAsync(string folderId, string name, CancellationToken ct);

    void Invalidate() { }
}
