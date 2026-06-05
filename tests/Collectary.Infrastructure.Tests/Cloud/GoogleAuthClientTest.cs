using Collectary.Core.Domain;
using Collectary.Infrastructure.Cloud.Auth;
using Collectary.Infrastructure.Tests.Infrastructure;
using Google.Apis.Auth.OAuth2.Responses;

namespace Collectary.Infrastructure.Tests.Cloud;

/// <summary>
/// <see cref="GoogleAuthClient"/>'s non-interactive surface: restoring a cached token, refreshing a
/// stale one, labelling the account from the (signature-validated) id token via an injected
/// <see cref="IIdTokenEmailReader"/>, and signing out. The interactive browser flow
/// (<c>SignInInteractiveAsync</c>) and the real Google token validation are excluded and verified
/// manually.
/// </summary>
[TestFixture]
public class GoogleAuthClientTest
{
    private InMemoryDataStore _store = null!;

    [SetUp]
    public void SetUp() => _store = new InMemoryDataStore();

    private sealed class StubEmailReader : IIdTokenEmailReader
    {
        private readonly string? _email;
        public StubEmailReader(string? email) => _email = email;
        public Task<string?> ReadEmailAsync(string? idToken, CancellationToken ct) => Task.FromResult(_email);
    }

    private GoogleAuthClient Build(string? email = null, StubHttpMessageHandler? stub = null) =>
        new("client-id", "client-secret", _store,
            stub is null ? null : new StubGoogleHttpClientFactory(stub),
            new StubEmailReader(email));

    // The flow caches the token under the "user" key; mirror that so the client can restore it.
    private Task StoreToken(TokenResponse token) => _store.StoreAsync("user", token);

    private static TokenResponse FreshToken(string accessToken = "at") => new()
    {
        AccessToken = accessToken,
        RefreshToken = "rt",
        ExpiresInSeconds = 3600,
        IssuedUtc = DateTime.UtcNow,
        IdToken = "header.payload.sig",
    };

    [Test]
    public void Provider_IsGoogleDrive() =>
        Assert.That(Build().Provider, Is.EqualTo(CloudProvider.GoogleDrive));

    [Test]
    public void IsSignedIn_BeforeRestore_False() =>
        Assert.That(Build().IsSignedIn, Is.False);

    [Test]
    public async Task GetAccessTokenAsync_NoStoredToken_ReturnsNull()
    {
        var sut = Build();

        Assert.Multiple(async () =>
        {
            Assert.That(await sut.GetAccessTokenAsync(CancellationToken.None), Is.Null);
            Assert.That(sut.IsSignedIn, Is.False);
        });
    }

    [Test]
    public async Task GetAccessTokenAsync_FreshStoredToken_ReturnsItAndSignsIn()
    {
        await StoreToken(FreshToken("stored-access"));
        var sut = Build();

        var token = await sut.GetAccessTokenAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(token, Is.EqualTo("stored-access"));
            Assert.That(sut.IsSignedIn, Is.True);
        });
    }

    [Test]
    public async Task GetAccessTokenAsync_LabelsAccountFromValidatedEmail()
    {
        await StoreToken(FreshToken());
        var sut = Build(email: "user@example.com");

        await sut.GetAccessTokenAsync(CancellationToken.None);

        Assert.That(sut.Account, Is.EqualTo("user@example.com"));
    }

    [Test]
    public async Task GetAccessTokenAsync_NoValidatedEmail_UsesDefaultLabel()
    {
        await StoreToken(FreshToken());
        var sut = Build(email: null);

        await sut.GetAccessTokenAsync(CancellationToken.None);

        Assert.That(sut.Account, Is.EqualTo("Google Drive"));
    }

    [Test]
    public async Task GetAccessTokenAsync_StaleToken_RefreshesAndReturnsNewToken()
    {
        await StoreToken(new TokenResponse
        {
            AccessToken = "old-access",
            RefreshToken = "rt",
            ExpiresInSeconds = 3600,
            IssuedUtc = DateTime.UtcNow.AddHours(-2), // expired ⇒ stale ⇒ must refresh
        });
        using var stub = new StubHttpMessageHandler();
        stub.OnJson(HttpMethod.Post, "token",
            """{"access_token":"refreshed-access","expires_in":3600,"token_type":"Bearer"}""");
        var sut = Build(stub: stub);

        var token = await sut.GetAccessTokenAsync(CancellationToken.None);

        Assert.That(token, Is.EqualTo("refreshed-access"));
    }

    [Test]
    public async Task GetAccessTokenAsync_CachesCredential_AfterFirstRestore()
    {
        await StoreToken(FreshToken("stored-access"));
        var sut = Build();
        await sut.GetAccessTokenAsync(CancellationToken.None); // caches the credential

        await _store.ClearAsync(); // a re-restore would now find nothing
        var second = await sut.GetAccessTokenAsync(CancellationToken.None);

        Assert.That(second, Is.EqualTo("stored-access"), "should reuse the cached credential, not re-restore");
    }

    [Test]
    public async Task SignOutAsync_ClearsStoreCredentialAndAccount()
    {
        await StoreToken(FreshToken());
        var sut = Build(email: "user@example.com");
        await sut.GetAccessTokenAsync(CancellationToken.None);

        await sut.SignOutAsync();

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsSignedIn, Is.False);
            Assert.That(sut.Account, Is.Null);
            Assert.That(_store.ClearCount, Is.EqualTo(1));
        });
    }
}
