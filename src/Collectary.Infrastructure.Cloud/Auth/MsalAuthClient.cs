using System.Diagnostics.CodeAnalysis;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Microsoft.Identity.Client;

namespace Collectary.Infrastructure.Cloud.Auth;

/// <summary>
/// OneDrive auth via MSAL. The interactive sign-in opens the system browser (desktop) or a Chrome
/// Custom Tab (Android); the platform-specific redirect, interactive parent and token cache come
/// from <see cref="MsalPlatformOptions"/>. Refresh is handled silently by MSAL.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Interactive OAuth requires a browser and a real account; verified manually.")]
public class MsalAuthClient : ICloudAuthClient
{
    private static readonly string[] Scopes = { "Files.ReadWrite", "User.Read" };

    private readonly IPublicClientApplication _app;
    private readonly MsalPlatformOptions _options;
    private IAccount? _account;

    public MsalAuthClient(string clientId, MsalPlatformOptions options)
    {
        _options = options;
        _app = PublicClientApplicationBuilder.Create(clientId)
            // "consumers" — personal Microsoft accounts only. They always have consumer OneDrive,
            // which avoids work/school tenants that lack a OneDrive/SharePoint license.
            .WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.PersonalMicrosoftAccount)
            .WithRedirectUri(options.RedirectUri)
            .Build();

        options.ConfigureTokenCache?.Invoke(_app);
    }

    public CloudProvider Provider => CloudProvider.OneDrive;

    public bool IsSignedIn => _account is not null;

    public string? Account => _account?.Username;

    public async Task SignInInteractiveAsync(CancellationToken ct)
    {
        var request = _app.AcquireTokenInteractive(Scopes)
            .WithUseEmbeddedWebView(false);
        if (_options.ParentActivityProvider is not null)
            request = request.WithParentActivityOrWindow(_options.ParentActivityProvider());
        var result = await request.ExecuteAsync(ct);
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
