using System.Diagnostics.CodeAnalysis;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;

namespace Collectary.Infrastructure.Cloud.Auth;

/// <summary>
/// Google Drive auth via Google.Apis. Interactive sign-in opens the system browser (loopback
/// redirect); tokens are persisted through the supplied <see cref="IDataStore"/>
/// (<see cref="DpapiDataStore"/> = DPAPI-encrypted). Uses the least-privilege <c>drive.file</c> scope.
/// The account email is read through an <see cref="IIdTokenEmailReader"/> that validates the
/// id_token's signature before any claim is trusted.
/// </summary>
public class GoogleAuthClient : ICloudAuthClient
{
    private const string DefaultAccountLabel = "Google Drive";
    // Stryker disable once all: the scope list only feeds the excluded interactive sign-in flow
    // (GoogleWebAuthorizationBroker); it is not exercised by the non-interactive token paths under test.
    private static readonly string[] Scopes = { DriveService.Scope.DriveFile, "email" };
    private const string UserId = "user";

    private readonly GoogleAuthorizationCodeFlow _flow;
    private readonly ClientSecrets _secrets;
    private readonly IDataStore _dataStore;
    private readonly IIdTokenEmailReader _emailReader;
    private UserCredential? _credential;
    private string? _account;

    public GoogleAuthClient(
        string clientId,
        string clientSecret,
        IDataStore dataStore,
        Google.Apis.Http.IHttpClientFactory? httpClientFactory = null,
        IIdTokenEmailReader? emailReader = null)
    {
        _secrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret };
        _dataStore = dataStore;
        _emailReader = emailReader ?? new GoogleIdTokenEmailReader();
        _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = _secrets,
            Scopes = Scopes,
            DataStore = dataStore,
            HttpClientFactory = httpClientFactory,
        });
    }

    public CloudProvider Provider => CloudProvider.GoogleDrive;

    public bool IsSignedIn => _credential is not null;

    public string? Account => _account;

    [ExcludeFromCodeCoverage(Justification = "GoogleWebAuthorizationBroker opens a browser and needs a real account; verified manually.")]
    public async Task SignInInteractiveAsync(CancellationToken ct)
    {
        _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(_secrets, Scopes, UserId, ct, _dataStore);
        _account = await _emailReader.ReadEmailAsync(_credential.Token.IdToken, ct) ?? DefaultAccountLabel;
    }

    public async Task TryRestoreSessionAsync(CancellationToken ct) => await GetAccessTokenAsync(ct);

    public async Task SignOutAsync()
    {
        await _dataStore.ClearAsync();
        _credential = null;
        _account = null;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        _credential ??= await RestoreCredentialAsync(ct);
        if (_credential is null) return null;

        if (_credential.Token.IsStale)
            await _credential.RefreshTokenAsync(ct);
        return _credential.Token.AccessToken;
    }

    private async Task<UserCredential?> RestoreCredentialAsync(CancellationToken ct)
    {
        var token = await _flow.LoadTokenAsync(UserId, ct);
        if (token is null) return null;
        // Stryker disable once all: equivalent mutant — _account is always null when this runs (the
        // credential is cached after the first restore), so "??=" and "=" behave identically here.
        _account ??= await _emailReader.ReadEmailAsync(token.IdToken, ct) ?? DefaultAccountLabel;
        return new UserCredential(_flow, UserId, token);
    }
}
