using System.Diagnostics.CodeAnalysis;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Collectary.Infrastructure.Cloud.Auth;

/// <summary>
/// OneDrive auth via MSAL. The interactive sign-in opens the system browser and uses MSAL's
/// built-in loopback redirect; tokens are cached by <c>Microsoft.Identity.Client.Extensions.Msal</c>
/// (DPAPI-encrypted on Windows). Refresh is handled silently by MSAL.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Interactive OAuth requires a browser and a real account; verified manually.")]
public class MsalAuthClient : ICloudAuthClient
{
    private static readonly string[] Scopes = { "Files.ReadWrite", "User.Read" };

    private readonly IPublicClientApplication _app;
    private IAccount? _account;

    public MsalAuthClient(string clientId, string cacheDirectory)
    {
        _app = PublicClientApplicationBuilder.Create(clientId)
            // "consumers" — personal Microsoft accounts only. They always have consumer OneDrive,
            // which avoids work/school tenants that lack a OneDrive/SharePoint license.
            .WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.PersonalMicrosoftAccount)
            .WithRedirectUri("http://localhost")
            .Build();

        var storage = new StorageCreationPropertiesBuilder("collectary_msal.cache", cacheDirectory).Build();
        MsalCacheHelper.CreateAsync(storage).GetAwaiter().GetResult().RegisterCache(_app.UserTokenCache);
    }

    public CloudProvider Provider => CloudProvider.OneDrive;

    public bool IsSignedIn => _account is not null;

    public string? Account => _account?.Username;

    public async Task SignInInteractiveAsync(CancellationToken ct)
    {
        var result = await _app.AcquireTokenInteractive(Scopes)
            .WithUseEmbeddedWebView(false)
            .ExecuteAsync(ct);
        _account = result.Account;
    }

    public async Task SignOutAsync()
    {
        foreach (var account in await _app.GetAccountsAsync())
            await _app.RemoveAsync(account);
        _account = null;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        var account = _account ?? (await _app.GetAccountsAsync()).FirstOrDefault();
        if (account is null) return null;

        try
        {
            var result = await _app.AcquireTokenSilent(Scopes, account).ExecuteAsync(ct);
            _account = result.Account;
            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            return null;
        }
    }
}
