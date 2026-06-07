using System.Collections.Concurrent;
using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Sync;

/// <summary>
/// The single <see cref="ISyncBackend"/> consumed by <c>SyncService</c>. It delegates every call to
/// the backend of the currently-selected <see cref="CloudProvider"/>, resolved fresh each call so
/// the user can switch providers at runtime without re-resolving the (singleton) sync service.
/// </summary>
public class RoutingSyncBackend : ISyncBackend
{
    private readonly Func<CloudProvider> _activeProvider;
    private readonly IReadOnlyDictionary<CloudProvider, Func<ISyncBackend>> _backends;
    private readonly ConcurrentDictionary<CloudProvider, ISyncBackend> _resolved = new();
    private readonly NullSyncBackend _fallback = new();

    public RoutingSyncBackend(Func<CloudProvider> activeProvider, IReadOnlyDictionary<CloudProvider, Func<ISyncBackend>> backends)
    {
        _activeProvider = activeProvider;
        _backends = backends;
    }

    private ISyncBackend Current
    {
        get
        {
            var provider = _activeProvider();
            if (!_backends.TryGetValue(provider, out var factory)) return _fallback;
            return _resolved.GetOrAdd(provider, _ => factory());
        }
    }

    public bool IsAvailable => Current.IsAvailable;

    public void Invalidate()
    {
        foreach (var backend in _resolved.Values)
            backend.Invalidate();
        _resolved.Clear();
    }

    public Task<IReadOnlyList<SyncEntry>> ListAsync(string kind) => Current.ListAsync(kind);

    public Task<string?> ReadAsync(string kind, Guid id) => Current.ReadAsync(kind, id);

    public Task<string?> ReadAtRevisionAsync(string kind, Guid id, long revision) =>
        Current.ReadAtRevisionAsync(kind, id, revision);

    public Task WriteAsync(string kind, Guid id, string content, long revision) =>
        Current.WriteAsync(kind, id, content, revision);

    public Task DeleteAsync(string kind, Guid id) => Current.DeleteAsync(kind, id);

    public Task<IReadOnlyList<string>> ListBlobKeysAsync(string kind) => Current.ListBlobKeysAsync(kind);

    public Task<byte[]?> ReadBlobAsync(string kind, string key) => Current.ReadBlobAsync(kind, key);

    public Task WriteBlobAsync(string kind, string key, byte[] content) =>
        Current.WriteBlobAsync(kind, key, content);

    public Task DeleteBlobAsync(string kind, string key) => Current.DeleteBlobAsync(kind, key);
}
