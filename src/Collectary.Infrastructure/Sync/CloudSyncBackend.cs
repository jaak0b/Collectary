using System.Collections.Concurrent;
using System.Text;
using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Sync;

/// <summary>
/// Adapts any <see cref="ICloudFileStore"/> to <see cref="ISyncBackend"/> by replicating the
/// FileSystem layout — <c>{kind}/{id:N}.{revision}.json</c> documents plus blobs under their kind
/// folder — so <c>SyncService</c> is unaware of which cloud provider is underneath.
/// </summary>
public class CloudSyncBackend : ISyncBackend
{
    private readonly ICloudFileStore _store;
    private readonly SyncFileNaming _naming = new();
    private readonly ConcurrentDictionary<string, string> _kindFolders = new();
    private readonly SemaphoreSlim _folderGate = new(1, 1);

    public CloudSyncBackend(ICloudFileStore store) => _store = store;

    public bool IsAvailable => _store.IsAvailable;

    public async Task<IReadOnlyList<SyncEntry>> ListAsync(string kind)
    {
        var folder = await KindFolderAsync(kind);
        var files = await _store.ListFilesAsync(folder, CancellationToken.None);

        var entries = new List<SyncEntry>();
        foreach (var file in files)
            if (_naming.TryParseDocument(file.Name, out var id, out var revision))
                entries.Add(new SyncEntry(id, revision));
        return entries;
    }

    public async Task<string?> ReadAsync(string kind, Guid id)
    {
        var folder = await KindFolderAsync(kind);
        var match = (await _store.ListFilesAsync(folder, CancellationToken.None))
            .FirstOrDefault(f => _naming.BelongsTo(f.Name, id));
        if (match is null) return null;

        var bytes = await _store.DownloadAsync(folder, match.Name, CancellationToken.None);
        return bytes is null ? null : Encoding.UTF8.GetString(bytes);
    }

    public async Task WriteAsync(string kind, Guid id, string content, long revision)
    {
        var folder = await KindFolderAsync(kind);
        var target = _naming.DocumentName(id, revision);
        await _store.UploadAsync(folder, target, Encoding.UTF8.GetBytes(content), CancellationToken.None);

        // Upload-then-delete: stale revisions of the same id are removed only after the new one lands.
        var stale = (await _store.ListFilesAsync(folder, CancellationToken.None))
            .Where(f => _naming.BelongsTo(f.Name, id)
                        && !string.Equals(f.Name, target, StringComparison.OrdinalIgnoreCase));
        foreach (var file in stale)
            await _store.DeleteAsync(folder, file.Name, CancellationToken.None);
    }

    public async Task DeleteAsync(string kind, Guid id)
    {
        var folder = await KindFolderAsync(kind);
        var matches = (await _store.ListFilesAsync(folder, CancellationToken.None))
            .Where(f => _naming.BelongsTo(f.Name, id));
        foreach (var file in matches)
            await _store.DeleteAsync(folder, file.Name, CancellationToken.None);
    }

    public async Task<IReadOnlyList<string>> ListBlobKeysAsync(string kind)
    {
        var folder = await KindFolderAsync(kind);
        return (await _store.ListFilesAsync(folder, CancellationToken.None))
            .Select(f => f.Name)
            .ToList();
    }

    public async Task<byte[]?> ReadBlobAsync(string kind, string key)
    {
        var folder = await KindFolderAsync(kind);
        return await _store.DownloadAsync(folder, _naming.SafeKey(key), CancellationToken.None);
    }

    public async Task WriteBlobAsync(string kind, string key, byte[] content)
    {
        var folder = await KindFolderAsync(kind);
        await _store.UploadAsync(folder, _naming.SafeKey(key), content, CancellationToken.None);
    }

    public async Task DeleteBlobAsync(string kind, string key)
    {
        var folder = await KindFolderAsync(kind);
        await _store.DeleteAsync(folder, _naming.SafeKey(key), CancellationToken.None);
    }

    private async Task<string> KindFolderAsync(string kind)
    {
        if (_kindFolders.TryGetValue(kind, out var cached)) return cached;

        await _folderGate.WaitAsync();
        try
        {
            if (_kindFolders.TryGetValue(kind, out cached)) return cached;
            var id = await _store.EnsureFolderAsync(_store.RootFolderId, kind, CancellationToken.None);
            _kindFolders[kind] = id;
            return id;
        }
        finally
        {
            _folderGate.Release();
        }
    }
}
