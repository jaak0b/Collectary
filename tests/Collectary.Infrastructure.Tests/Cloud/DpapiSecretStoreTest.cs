using System.Runtime.Versioning;
using System.Text;
using Collectary.Infrastructure.Cloud.Auth;

namespace Collectary.Infrastructure.Tests.Cloud;

[TestFixture]
[Platform("Win")]
[SupportedOSPlatform("windows")]
public class DpapiSecretStoreTest : FileSystemTestBase
{
    private DpapiSecretStore _sut = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _sut = new DpapiSecretStore(TempDir);
    }

    [Test]
    public void SetThenGet_RoundTrips()
    {
        _sut.Set("token", "s3cret-refresh-token");

        Assert.That(_sut.Get("token"), Is.EqualTo("s3cret-refresh-token"));
    }

    [Test]
    public void Get_MissingKey_ReturnsNull() =>
        Assert.That(_sut.Get("absent"), Is.Null);

    [Test]
    public void Set_Twice_OverwritesValue()
    {
        _sut.Set("token", "first");
        _sut.Set("token", "second");

        Assert.That(_sut.Get("token"), Is.EqualTo("second"));
    }

    [Test]
    public void Delete_RemovesValue()
    {
        _sut.Set("token", "value");

        _sut.Delete("token");

        Assert.That(_sut.Get("token"), Is.Null);
    }

    [Test]
    public void Delete_MissingKey_DoesNotThrow() =>
        Assert.That(() => _sut.Delete("absent"), Throws.Nothing);

    [Test]
    public void Set_StoresEncrypted_NotPlaintext()
    {
        const string secret = "plaintext-should-not-appear-on-disk";
        _sut.Set("token", secret);

        var onDisk = Directory.EnumerateFiles(TempDir)
            .SelectMany(File.ReadAllBytes)
            .ToArray();

        Assert.That(Encoding.UTF8.GetString(onDisk), Does.Not.Contain(secret));
    }

    [TestCase("../escape")]
    [TestCase("a/b")]
    [TestCase("")]
    [TestCase(".")]
    [TestCase("..")]
    [TestCase("a*b")]
    [TestCase("*ab")] // invalid char at index 0 — guards the ">= 0" (not "> 0") boundary
    public void Set_UnsafeKey_Throws(string key) =>
        Assert.That(() => _sut.Set(key, "x"),
            Throws.InstanceOf<ArgumentException>().With.Message.Contains("Unsafe secret key"));

    [Test]
    public void Set_WritesFileWithSecretExtension()
    {
        _sut.Set("token", "value");

        Assert.That(File.Exists(Path.Combine(TempDir, "token.secret")), Is.True);
    }

    [Test]
    public void Clear_RemovesSecretFiles_ButLeavesOthers()
    {
        _sut.Set("a", "1");
        _sut.Set("b", "2");
        var foreign = Path.Combine(TempDir, "keep.txt");
        File.WriteAllText(foreign, "not a secret");

        _sut.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(_sut.Get("a"), Is.Null);
            Assert.That(_sut.Get("b"), Is.Null);
            Assert.That(File.Exists(foreign), Is.True, "non-secret files must be left alone");
        });
    }

    [Test]
    public void Clear_MissingDirectory_DoesNotThrow()
    {
        var store = new DpapiSecretStore(Path.Combine(TempDir, "does-not-exist"));

        Assert.That(() => store.Clear(), Throws.Nothing);
    }
}
