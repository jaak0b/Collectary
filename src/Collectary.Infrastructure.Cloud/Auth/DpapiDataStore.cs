using System.Runtime.Versioning;
using System.Text;
using Google.Apis.Json;
using Google.Apis.Util.Store;

namespace Collectary.Infrastructure.Cloud.Auth;

/// <summary>
/// Google <see cref="IDataStore"/> backed by <see cref="DpapiSecretStore"/>, so OAuth tokens are
/// DPAPI-encrypted at rest instead of written as plaintext by the SDK's default <c>FileDataStore</c>.
/// Google keys can contain characters illegal in file names, so they're base64url-encoded first.
/// </summary>
[SupportedOSPlatform("windows")]
public class DpapiDataStore : IDataStore
{
    private readonly DpapiSecretStore _store;

    public DpapiDataStore(DpapiSecretStore store) => _store = store;

    public Task StoreAsync<T>(string key, T value)
    {
        _store.Set(Encode(key), NewtonsoftJsonSerializer.Instance.Serialize(value));
        return Task.CompletedTask;
    }

    public Task<T> GetAsync<T>(string key)
    {
        var json = _store.Get(Encode(key));
        return Task.FromResult(json is null ? default! : NewtonsoftJsonSerializer.Instance.Deserialize<T>(json));
    }

    public Task DeleteAsync<T>(string key)
    {
        _store.Delete(Encode(key));
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _store.Clear();
        return Task.CompletedTask;
    }

    private static string Encode(string key) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
