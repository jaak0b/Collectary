using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace Collectary.Infrastructure.Cloud.Auth;

/// <summary>
/// Stores small secrets (OAuth refresh tokens) encrypted at rest with Windows DPAPI
/// (<c>CurrentUser</c> scope), one file per key. Used by the Google auth client, whose default
/// token store writes plaintext.
/// </summary>
[SupportedOSPlatform("windows")]
public class DpapiSecretStore
{
    private readonly string _directory;

    public DpapiSecretStore(string directory) => _directory = directory;

    public void Set(string key, string value)
    {
        var path = PathFor(key);
        Directory.CreateDirectory(_directory);
        var cipher = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(path, cipher);
    }

    public string? Get(string key)
    {
        var path = PathFor(key);
        if (!File.Exists(path)) return null;
        var plain = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plain);
    }

    public void Delete(string key)
    {
        var path = PathFor(key);
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>Removes every secret in this store's directory.</summary>
    public void Clear()
    {
        if (!Directory.Exists(_directory)) return;
        foreach (var file in Directory.EnumerateFiles(_directory, "*.secret"))
            File.Delete(file);
    }

    private string PathFor(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key is "." or ".."
            || Path.GetFileName(key) != key
            || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException($"Unsafe secret key: '{key}'", nameof(key));
        return Path.Combine(_directory, $"{key}.secret");
    }
}
