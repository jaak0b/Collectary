using Google.Apis.Util.Store;

namespace Collectary.Infrastructure.Tests.Infrastructure;

/// <summary>
/// In-memory Google <see cref="IDataStore"/> for tests — stands in for the DPAPI store so the
/// OAuth token cache never touches disk.
/// </summary>
public sealed class InMemoryDataStore : IDataStore
{
    private readonly Dictionary<string, object?> _items = new();

    public int ClearCount { get; private set; }

    public Task StoreAsync<T>(string key, T value)
    {
        _items[key] = value;
        return Task.CompletedTask;
    }

    public Task DeleteAsync<T>(string key)
    {
        _items.Remove(key);
        return Task.CompletedTask;
    }

    public Task<T> GetAsync<T>(string key) =>
        Task.FromResult(_items.TryGetValue(key, out var value) && value is T typed ? typed : default!);

    public Task ClearAsync()
    {
        ClearCount++;
        _items.Clear();
        return Task.CompletedTask;
    }
}
