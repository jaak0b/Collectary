using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Sync;

public class FileSystemSyncBackend : ISyncBackend
{
    private readonly Func<string?> _rootProvider;

    public FileSystemSyncBackend(string rootPath) : this(() => rootPath) { }

    public FileSystemSyncBackend(Func<string?> rootProvider) => _rootProvider = rootProvider;

    private string Root => _rootProvider() ?? string.Empty;

    public bool IsAvailable => !string.IsNullOrWhiteSpace(Root);

    public Task<IReadOnlyList<SyncEntry>> ListAsync(string kind)
    {
        var dir = KindDir(kind);
        if (!Directory.Exists(dir)) return Task.FromResult<IReadOnlyList<SyncEntry>>(Array.Empty<SyncEntry>());

        var entries = new List<SyncEntry>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var dot = name.LastIndexOf('.');
            if (dot <= 0) continue;
            if (Guid.TryParse(name[..dot], out var id) && long.TryParse(name[(dot + 1)..], out var revision))
                entries.Add(new SyncEntry(id, revision));
        }

        return Task.FromResult<IReadOnlyList<SyncEntry>>(entries);
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
        var target = Path.Combine(dir, $"{id:N}.{revision}.json");

        foreach (var stale in Directory.EnumerateFiles(dir, $"{id:N}.*.json"))
            if (!string.Equals(stale, target, StringComparison.OrdinalIgnoreCase))
                File.Delete(stale);

        var temp = target + ".tmp";
        await File.WriteAllTextAsync(temp, content);
        File.Move(temp, target, overwrite: true);
    }

    public Task DeleteAsync(string kind, Guid id)
    {
        var dir = KindDir(kind);
        if (Directory.Exists(dir))
            foreach (var file in Directory.EnumerateFiles(dir, $"{id:N}.*.json"))
                File.Delete(file);
        return Task.CompletedTask;
    }

    private string? FindFile(string kind, Guid id)
    {
        var dir = KindDir(kind);
        return Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir, $"{id:N}.*.json").FirstOrDefault()
            : null;
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

    private string SafeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key is "." or ".."
            || Path.GetFileName(key) != key
            || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException($"Unsafe blob key: '{key}'", nameof(key));
        return key;
    }

    private string KindDir(string kind) => Path.Combine(Root, kind);
}
