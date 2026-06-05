namespace Collectary.Infrastructure.Cloud.Auth;

/// <summary>
/// Builds the Android OneDrive <see cref="MsalPlatformOptions"/>: the <c>msauth://{package}/{hash}</c>
/// redirect MSAL's BrowserTabActivity catches, the current <c>Activity</c> as the Custom Tab parent,
/// and no token-cache configurator (MSAL uses its built-in Android Keystore-backed cache). The
/// signature hash is URL-encoded because the SHA-1/base64 hash can contain <c>+ / =</c>.
/// </summary>
public sealed class AndroidMsalPlatformOptionsFactory
{
    private readonly string _applicationId;
    private readonly string _signatureHash;
    private readonly Func<object?> _parentActivityProvider;

    public AndroidMsalPlatformOptionsFactory(
        string applicationId, string signatureHash, Func<object?> parentActivityProvider)
    {
        _applicationId = applicationId;
        _signatureHash = signatureHash;
        _parentActivityProvider = parentActivityProvider;
    }

    public MsalPlatformOptions Create() =>
        new(
            $"msauth://{_applicationId}/{Uri.EscapeDataString(_signatureHash)}",
            _parentActivityProvider,
            ConfigureTokenCache: null);
}
