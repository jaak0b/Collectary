using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
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
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Interactive OAuth requires a browser and a real account; verified manually.")]
public class GoogleAuthClient : ICloudAuthClient
{
    private static readonly string[] Scopes = { DriveService.Scope.DriveFile, "email" };
    private const string UserId = "user";

    private readonly GoogleAuthorizationCodeFlow _flow;
    private readonly ClientSecrets _secrets;
    private readonly IDataStore _dataStore;
    private UserCredential? _credential;
    private string? _account;

    public GoogleAuthClient(string clientId, string clientSecret, IDataStore dataStore)
    {
        _secrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret };
        _dataStore = dataStore;
        _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = _secrets,
            Scopes = Scopes,
            DataStore = dataStore,
        });
    }

    public CloudProvider Provider => CloudProvider.GoogleDrive;

    public bool IsSignedIn => _credential is not null;

    public string? Account => _account;

    public async Task SignInInteractiveAsync(CancellationToken ct)
    {
        _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(_secrets, Scopes, UserId, ct, _dataStore);
        _account = EmailFromIdToken(_credential.Token.IdToken) ?? "Google Drive";
    }

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
        _account ??= EmailFromIdToken(token.IdToken) ?? "Google Drive";
        return new UserCredential(_flow, UserId, token);
    }

    private static string? EmailFromIdToken(string? idToken)
    {
        if (string.IsNullOrEmpty(idToken)) return null;
        var parts = idToken.Split('.');
        if (parts.Length < 2) return null;

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload += (payload.Length % 4) switch { 2 => "==", 3 => "=", _ => string.Empty };

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("email", out var email) ? email.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
