using System.Diagnostics.CodeAnalysis;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Collectary.Infrastructure.Cloud.Auth;

/// <summary>
/// Builds the desktop OneDrive <see cref="MsalPlatformOptions"/>: MSAL's loopback redirect plus the
/// cross-platform <c>MsalCacheHelper</c> token cache (DPAPI-encrypted on Windows). No interactive
/// parent window is needed — the system browser handles sign-in.
/// </summary>
public sealed class DesktopMsalPlatformOptionsFactory
{
    private readonly string _tokenCacheDirectory;

    public DesktopMsalPlatformOptionsFactory(string tokenCacheDirectory) =>
        _tokenCacheDirectory = tokenCacheDirectory;

    public MsalPlatformOptions Create() =>
        new("http://localhost", ParentActivityProvider: null, RegisterTokenCache);

    [ExcludeFromCodeCoverage(Justification = "MsalCacheHelper needs the real per-user secure store; verified manually.")]
    private void RegisterTokenCache(IPublicClientApplication app)
    {
        var storage = new StorageCreationPropertiesBuilder("collectary_msal.cache", _tokenCacheDirectory).Build();
        MsalCacheHelper.CreateAsync(storage).GetAwaiter().GetResult().RegisterCache(app.UserTokenCache);
    }
}
