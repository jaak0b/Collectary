using Collectary.Core.Ports;
using Collectary.Infrastructure.Persistence;

namespace Collectary.Infrastructure.Tests.Repositories;

[TestFixture]
public class CredentialStoreTest : DbIntegrationTestBase
{
    private CredentialStore _sut = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _sut = new CredentialStore(DbFactory);
    }

    [Test]
    public async Task SaveAsync_ThenGet_RoundTripsAllFields()
    {
        var userId = Guid.NewGuid();
        var stored = new PasswordHash(new byte[] { 1, 2, 3 }, new byte[] { 4, 5 }, 210_000, "PBKDF2-HMAC-SHA512");

        await _sut.SaveAsync(userId, stored);
        var loaded = await _sut.GetAsync(userId);

        Assert.That(loaded, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(loaded!.Hash, Is.EqualTo(stored.Hash));
            Assert.That(loaded.Salt, Is.EqualTo(stored.Salt));
            Assert.That(loaded.Iterations, Is.EqualTo(stored.Iterations));
            Assert.That(loaded.Algorithm, Is.EqualTo(stored.Algorithm));
        });
    }

    [Test]
    public async Task GetAsync_WhenMissing_ReturnsNull() =>
        Assert.That(await _sut.GetAsync(Guid.NewGuid()), Is.Null);

    [Test]
    public async Task SaveAsync_WhenExisting_Overwrites()
    {
        var userId = Guid.NewGuid();
        await _sut.SaveAsync(userId, new PasswordHash(new byte[] { 1 }, new byte[] { 1 }, 1, "PBKDF2-HMAC-SHA512"));

        var updated = new PasswordHash(new byte[] { 9 }, new byte[] { 8 }, 210_000, "PBKDF2-HMAC-SHA512");
        await _sut.SaveAsync(userId, updated);

        var loaded = await _sut.GetAsync(userId);
        Assert.That(loaded!.Hash, Is.EqualTo(updated.Hash));
        Assert.That(loaded.Iterations, Is.EqualTo(210_000));
    }
}
