using Collectary.Infrastructure.Sync;

namespace Collectary.Infrastructure.Tests.Sync;

[TestFixture]
public class SyncFileNamingTest
{
    private SyncFileNaming _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new SyncFileNaming();

    [Test]
    public void DocumentName_IsTheFlatIdName()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-0000000000ab");

        Assert.That(_sut.DocumentName(id), Is.EqualTo("000000000000000000000000000000ab.json"));
    }

    [Test]
    public void TryParseId_FlatName_ParsesTheId()
    {
        var id = Guid.NewGuid();

        var ok = _sut.TryParseId(_sut.DocumentName(id), out var parsedId);

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.True);
            Assert.That(parsedId, Is.EqualTo(id));
        });
    }

    [TestCase("noseparator.json")]
    [TestCase(".5.json")]
    [TestCase("not-a-guid.json")]
    [TestCase("00000000000000000000000000000000.5.json")]
    public void TryParseId_MalformedOrLegacyName_ReturnsFalse(string name) =>
        Assert.That(_sut.TryParseId(name, out _), Is.False);

    [Test]
    public void BelongsTo_SameId_True()
    {
        var id = Guid.NewGuid();

        Assert.That(_sut.BelongsTo(_sut.DocumentName(id), id), Is.True);
    }

    [Test]
    public void BelongsTo_LegacyRevisionName_True()
    {
        var id = Guid.NewGuid();

        Assert.That(_sut.BelongsTo($"{id:N}.5.json", id), Is.True);
    }

    [Test]
    public void BelongsTo_DifferentId_False() =>
        Assert.That(_sut.BelongsTo(_sut.DocumentName(Guid.NewGuid()), Guid.NewGuid()), Is.False);

    [Test]
    public void BelongsTo_NonJsonFile_False()
    {
        var id = Guid.NewGuid();

        Assert.That(_sut.BelongsTo($"{id:N}.png", id), Is.False);
    }

    [Test]
    public void SafeKey_PlainFileName_ReturnsKey() =>
        Assert.That(_sut.SafeKey("image.png"), Is.EqualTo("image.png"));

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(".")]
    [TestCase("..")]
    [TestCase("a/b")]
    [TestCase("a\\b")]
    [TestCase("../escape")]
    public void SafeKey_UnsafeKey_Throws(string key) =>
        Assert.That(() => _sut.SafeKey(key), Throws.InstanceOf<ArgumentException>());

    [Test]
    public void SafeKey_KeyWithInvalidChar_Throws() =>
        Assert.That(() => _sut.SafeKey("bad\0name"), Throws.InstanceOf<ArgumentException>());

    // These chars are illegal in a Windows file name but legal on Linux/Android; the key must be
    // rejected the same way on every platform so a blob stored on one device resolves on another.
    [TestCase("a:b.png")]
    [TestCase("a*b.png")]
    [TestCase("a?b.png")]
    [TestCase("a<b.png")]
    [TestCase("a>b.png")]
    [TestCase("a|b.png")]
    [TestCase("a\"b.png")]
    public void SafeKey_WindowsReservedChar_ThrowsOnEveryPlatform(string key) =>
        Assert.That(() => _sut.SafeKey(key), Throws.InstanceOf<ArgumentException>());
}
