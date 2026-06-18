using System.Linq;
using System.Reflection;

namespace Collectary.UI.Android;

/// <summary>
/// Android-specific OneDrive sign-in configuration. The OneDrive client id and the
/// <see cref="SignatureHash"/> (the URL-safe base64 SHA-1 of the APK signing certificate) are baked
/// into this assembly at build time from the <c>COLLECTARY_ONEDRIVE_CLIENT_ID</c> and the
/// configuration-specific signature-hash build environment variables (the release hash for Release,
/// the debug-keystore hash for Debug), because the phone has no access to the developer's environment
/// at runtime. The hash must match the <c>msauth://</c> redirect registered for that build's
/// application id in the Azure app registration; MSAL prints the expected value in its error message
/// on the first sign-in attempt if it is wrong.
/// </summary>
public class AndroidCloudConfig
{
    public const string SignatureHashPlaceholder = "<ANDROID_SIGNATURE_HASH>";

    public string SignatureHash => Baked("CollectaryAndroidSignatureHash") ?? SignatureHashPlaceholder;

    public string? OneDriveClientId => Baked("CollectaryOneDriveClientId");

    private string? Baked(string key)
    {
        var value = typeof(AndroidCloudConfig).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
