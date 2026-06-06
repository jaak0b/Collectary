using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Sync;

public class FileSystemSyncBackend : ISyncBackend
{
    private readonly Func<string?> _rootProvider;
    private readonly SyncFileNaming _naming = new();

    public FileSystemSyncBackend(string rootPath) : this(() => rootPath) { }

    public FileSystemSyncBackend(Func<string?> rootProvider) => _rootProvider = rootProvider;

    private string Root => _rootProvider() ?? string.Empty;

    public bool IsAvailable => !string.IsNullOrWhiteSpace(Root);

    public Task<IReadOnlyList<SyncEntry>> ListAsync(string kind)
    {
        var dir = KindDir(kind);
        if (!Directory.Exists(dir)) return Task.FromResult<IReadOnlyList<SyncEntry>>(Array.Empty<SyncEntry>());

        var highest = new Dictionary<Guid, long>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
            if (_naming.TryParseDocument(Path.GetFileName(file), out var id, out var revision)
                && (!highest.TryGetValue(id, out var current) || revision > current))
                highest[id] = revision;

        IReadOnlyList<SyncEntry> entries = highest.Select(kv => new SyncEntry(kv.Key, kv.Value)).ToList();
        return Task.FromResult(entries);
    }

    public async Task<string?> ReadAsync(string kind, Guid id)
    {
        var file = FindFile(kind, id);
        return file is not null ? await File.ReadAllTextAsync(file) : null;
    }

    public async Task WriteAsync(string kind, Guid id, string content, long revision)
    {
        var dir = KindDir(kind);
        Directory.CreateDirectory(dir);
        var target = Path.Combine(dir, _naming.DocumentName(id, revision));

        var temp = target + ".tmp";
        await File.WriteAllTextAsync(temp, content);
        File.Move(temp, target, overwrite: true);

        foreach (var stale in Directory.EnumerateFiles(dir, "*.json"))
            if (_naming.BelongsTo(Path.GetFileName(stale), id)
                && !string.Equals(stale, target, StringComparison.OrdinalIgnoreCase))
                File.Delete(stale);
    }

    public Task DeleteAsync(string kind, Guid id)
    {
        var dir = KindDir(kind);
        if (Directory.Exists(dir))
            foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
                if (_naming.BelongsTo(Path.GetFileName(file), id))
                    File.Delete(file);
        return Task.CompletedTask;
    }

    private string? FindFile(string kind, Guid id)
    {
        var dir = KindDir(kind);
        if (!Directory.Exists(dir)) return null;

        string? best = null;
        var bestRevision = -1L;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
            if (_naming.TryParseDocument(Path.GetFileName(file), out var fileId, out var revision)
                && fileId == id && revision > bestRevision)
            {
                best = file;
                bestRevision = revision;
            }
        return best;
    }

    public Task<IReadOnlyList<string>> ListBlobKeysAsync(string kind)
    {
        var dir = KindDir(kind);
        IReadOnlyList<string> keys = Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir)
                .Select(Path.GetFileName)
                .Where(n => n is not null && !n.EndsWith(".tmp", StringComparison.Ordinal))
                .Select(n => n!)
                .ToList()
            : Array.Empty<string>();
        return Task.FromResult(keys);
    }

    public async Task<byte[]?> ReadBlobAsync(string kind, string key)
    {
        var path = Path.Combine(KindDir(kind), SafeKey(key));
        return File.Exists(path) ? await File.ReadAllBytesAsync(path) : null;
    }

    public async Task WriteBlobAsync(string kind, string key, byte[] content)
    {
        var dir = KindDir(kind);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, SafeKey(key));
        var temp = path + ".tmp";
        await File.WriteAllBytesAsync(temp, content);
        File.Move(temp, path, overwrite: true);
    }

    public Task DeleteBlobAsync(string kind, string key)
    {
        var path = Path.Combine(KindDir(kind), SafeKey(key));
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string SafeKey(string key) => _naming.SafeKey(key);

    private string KindDir(string kind) => Path.Combine(Root, kind);
}
