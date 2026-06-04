using Collectary.Core.Ports;

namespace Collectary.Infrastructure.Sync;

/// <summary>
/// An <see cref="ISyncBackend"/> that is never available and silently ignores writes. Used as the
/// fallback when the selected provider has no backend registered (e.g. a cloud provider on a
/// platform where its SDK is not present).
/// </summary>
public class NullSyncBackend : ISyncBackend
{
    public bool IsAvailable => false;

    public Task<IReadOnlyList<SyncEntry>> ListAsync(string kind) =>
        Task.FromResult<IReadOnlyList<SyncEntry>>(Array.Empty<SyncEntry>());

    public Task<string?> ReadAsync(string kind, Guid id) => Task.FromResult<string?>(null);

    public Task WriteAsync(string kind, Guid id, string content, long revision) => Task.CompletedTask;

    public Task DeleteAsync(string kind, Guid id) => Task.CompletedTask;

    public Task<IReadOnlyList<string>> ListBlobKeysAsync(string kind) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

    public Task<byte[]?> ReadBlobAsync(string kind, string key) => Task.FromResult<byte[]?>(null);

    public Task WriteBlobAsync(string kind, string key, byte[] content) => Task.CompletedTask;

    public Task DeleteBlobAsync(string kind, string key) => Task.CompletedTask;
}
