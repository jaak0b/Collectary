using Collectary.Core.Ports;
using Collectary.Infrastructure.Security;

namespace Collectary.Infrastructure.Tests.Security;

[TestFixture]
public class Pbkdf2CredentialHasherTest
{
    private Pbkdf2CredentialHasher _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new Pbkdf2CredentialHasher();

    [Test]
    public void Hash_ThenVerify_WithCorrectPassword_ReturnsTrue()
    {
        var stored = _sut.Hash("correct horse battery staple");

        Assert.That(_sut.Verify("correct horse battery staple", stored), Is.True);
    }

    [Test]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var stored = _sut.Hash("correct horse battery staple");

        Assert.That(_sut.Verify("wrong password", stored), Is.False);
    }

    [Test]
    public void Hash_ProducesDifferentSaltAndHashPerCall()
    {
        var a = _sut.Hash("same password");
        var b = _sut.Hash("same password");

        Assert.Multiple(() =>
        {
            Assert.That(a.Salt, Is.Not.EqualTo(b.Salt));
            Assert.That(a.Hash, Is.Not.EqualTo(b.Hash));
        });
    }

    [Test]
    public void Hash_SetsAlgorithmAndIterations()
    {
        var stored = _sut.Hash("pw");

        Assert.Multiple(() =>
        {
            Assert.That(stored.Algorithm, Is.EqualTo("PBKDF2-HMAC-SHA512"));
            Assert.That(stored.Iterations, Is.GreaterThanOrEqualTo(210_000));
            Assert.That(stored.Hash, Has.Length.EqualTo(64));
            Assert.That(stored.Salt, Has.Length.EqualTo(16));
        });
    }

    [Test]
    public void Verify_WithUnsupportedAlgorithm_Throws()
    {
        var stored = new PasswordHash(new byte[64], new byte[16], 1000, "bcrypt");

        Assert.Throws<NotSupportedException>(() => _sut.Verify("pw", stored));
    }
}
