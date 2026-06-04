using System.Runtime.Versioning;
using Collectary.Infrastructure.Cloud.Auth;

namespace Collectary.Infrastructure.Tests.Cloud;

[TestFixture]
[Platform("Win")]
[SupportedOSPlatform("windows")]
public class DpapiDataStoreTest : FileSystemTestBase
{
    private DpapiDataStore _sut = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _sut = new DpapiDataStore(new DpapiSecretStore(TempDir));
    }

    private sealed record Token(string AccessToken, string RefreshToken);

    [Test]
    public async Task StoreThenGet_RoundTripsValue()
    {
        await _sut.StoreAsync("user", new Token("a", "r"));

        var loaded = await _sut.GetAsync<Token>("user");

        Assert.Multiple(() =>
        {
            Assert.That(loaded.AccessToken, Is.EqualTo("a"));
            Assert.That(loaded.RefreshToken, Is.EqualTo("r"));
        });
    }

    [Test]
    public async Task GetAsync_MissingKey_ReturnsDefault() =>
        Assert.That(await _sut.GetAsync<Token>("absent"), Is.Null);

    [Test]
    public async Task DeleteAsync_RemovesValue()
    {
        await _sut.StoreAsync("user", new Token("a", "r"));

        await _sut.DeleteAsync<Token>("user");

        Assert.That(await _sut.GetAsync<Token>("user"), Is.Null);
    }

    [Test]
    public async Task ClearAsync_RemovesEverything()
    {
        await _sut.StoreAsync("u1", new Token("a", "r"));
        await _sut.StoreAsync("u2", new Token("b", "s"));

        await _sut.ClearAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_sut.GetAsync<Token>("u1").Result, Is.Null);
            Assert.That(_sut.GetAsync<Token>("u2").Result, Is.Null);
        });
    }

    [Test]
    public async Task StoreAsync_KeyWithFilesystemUnsafeChars_StillRoundTrips()
    {
        // Google uses keys like "...TokenResponse-user:scope" with chars invalid in file names.
        const string key = "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user@x.com:scope/read";
        await _sut.StoreAsync(key, new Token("a", "r"));

        Assert.That((await _sut.GetAsync<Token>(key)).AccessToken, Is.EqualTo("a"));
    }
}
