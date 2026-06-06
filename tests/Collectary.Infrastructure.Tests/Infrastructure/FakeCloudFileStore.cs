using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Tests.Infrastructure;

/// <summary>
/// In-memory <see cref="ICloudFileStore"/> for testing <c>CloudSyncBackend</c> with no network.
/// Models a folder graph rooted at <see cref="RootFolderId"/> plus per-folder named blobs.
/// </summary>
public class FakeCloudFileStore : ICloudFileStore
{
    private sealed record FolderNode(string Id, string Name, string ParentId);

    private readonly Dictionary<string, FolderNode> _folders = new();
    private readonly Dictionary<(string FolderId, string Name), byte[]> _files = new();
    private int _seq;

    public FakeCloudFileStore() => _folders[RootFolderId] = new FolderNode(RootFolderId, string.Empty, string.Empty);

    public bool IsAvailable { get; set; } = true;

    public string RootFolderId { get; set; } = "root";

    public int EnsureFolderCalls { get; private set; }

    public int InvalidateCalls { get; private set; }

    public void Invalidate() => InvalidateCalls++;

    public Task<string> EnsureFolderAsync(string parentFolderId, string name, CancellationToken ct)
    {
        EnsureFolderCalls++;
        var existing = _folders.Values.FirstOrDefault(f => f.ParentId == parentFolderId && f.Name == name);
        if (existing is not null) return Task.FromResult(existing.Id);

        var id = $"folder-{++_seq}";
        _folders[id] = new FolderNode(id, name, parentFolderId);
        return Task.FromResult(id);
    }

    public Task<IReadOnlyList<CloudFile>> ListFilesAsync(string folderId, CancellationToken ct)
    {
        IReadOnlyList<CloudFile> files = _files
            .Where(kv => kv.Key.FolderId == folderId)
            .Select(kv => new CloudFile(kv.Key.Name, kv.Key.Name, kv.Value.Length))
            .ToList();
        return Task.FromResult(files);
    }

    public Task<IReadOnlyList<CloudFolder>> ListFoldersAsync(string folderId, CancellationToken ct)
    {
        IReadOnlyList<CloudFolder> folders = _folders.Values
            .Where(f => f.ParentId == folderId)
            .Select(f => new CloudFolder(f.Id, f.Name))
            .ToList();
        return Task.FromResult(folders);
    }

    public Task<byte[]?> DownloadAsync(string folderId, string name, CancellationToken ct) =>
        Task.FromResult(_files.TryGetValue((folderId, name), out var bytes) ? bytes : null);

    public Task UploadAsync(string folderId, string name, byte[] content, CancellationToken ct)
    {
        _files[(folderId, name)] = content;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string folderId, string name, CancellationToken ct)
    {
        _files.Remove((folderId, name));
        return Task.CompletedTask;
    }
}
