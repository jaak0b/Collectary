namespace Collectary.Core.Ports;

public interface ISyncBackend
{
    bool IsAvailable { get; }
    Task<IReadOnlyList<Guid>> ListAsync(string kind);
    Task<string?> ReadAsync(string kind, Guid id);
    Task WriteAsync(string kind, Guid id, string content);
    Task DeleteAsync(string kind, Guid id);

    Task<IReadOnlyList<string>> ListBlobKeysAsync(string kind);
    Task<byte[]?> ReadBlobAsync(string kind, string key);
    Task WriteBlobAsync(string kind, string key, byte[] content);
    Task DeleteBlobAsync(string kind, string key);

    void Invalidate() { }
}
