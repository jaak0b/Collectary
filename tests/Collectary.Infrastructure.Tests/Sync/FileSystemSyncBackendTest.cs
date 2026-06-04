using Collectary.Infrastructure.Sync;

namespace Collectary.Infrastructure.Tests.Sync;

[TestFixture]
public class FileSystemSyncBackendTest : FileSystemTestBase
{
    private FileSystemSyncBackend _sut = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _sut = new FileSystemSyncBackend(TempDir);
    }

    [Test]
    public async Task WriteAsync_ThenReadAsync_RoundTrips()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("presets", id, "{\"hello\":1}", 1);

        Assert.That(await _sut.ReadAsync("presets", id), Is.EqualTo("{\"hello\":1}"));
    }

    [Test]
    public async Task ReadAsync_WhenMissing_ReturnsNull() =>
        Assert.That(await _sut.ReadAsync("presets", Guid.NewGuid()), Is.Null);

    [Test]
    public async Task ListAsync_ReturnsAllDocumentsOfKind()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await _sut.WriteAsync("items", a, "A", 1);
        await _sut.WriteAsync("items", b, "B", 1);
        await _sut.WriteAsync("presets", Guid.NewGuid(), "X", 1);

        var items = await _sut.ListAsync("items");

        Assert.That(items.Select(d => d.Id), Is.EquivalentTo(new[] { a, b }));
    }

    [Test]
    public async Task ListAsync_ReturnsRevisionFromFilename()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("items", id, "A", 7);

        var entry = (await _sut.ListAsync("items")).Single();

        Assert.That(entry.Revision, Is.EqualTo(7));
    }

    [Test]
    public async Task WriteAsync_NewRevision_ReplacesOldFileWithoutDuplicating()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("items", id, "old", 1);
        await _sut.WriteAsync("items", id, "new", 2);

        var entries = await _sut.ListAsync("items");
        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries.Single().Revision, Is.EqualTo(2));
            Assert.That(_sut.ReadAsync("items", id).Result, Is.EqualTo("new"));
        });
    }

    [Test]
    public async Task ListAsync_WhenKindMissing_ReturnsEmpty() =>
        Assert.That(await _sut.ListAsync("nope"), Is.Empty);

    [Test]
    public async Task WriteAsync_Overwrites()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("presets", id, "first", 1);
        await _sut.WriteAsync("presets", id, "second", 1);

        Assert.That(await _sut.ReadAsync("presets", id), Is.EqualTo("second"));
    }

    [Test]
    public async Task DeleteAsync_RemovesDocument()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("presets", id, "x", 1);

        await _sut.DeleteAsync("presets", id);

        Assert.That(await _sut.ReadAsync("presets", id), Is.Null);
    }

    [Test]
    public async Task WriteBlobAsync_ThenReadBlob_RoundTrips()
    {
        await _sut.WriteBlobAsync("images", "pic.png", new byte[] { 1, 2, 3 });

        Assert.That(await _sut.ReadBlobAsync("images", "pic.png"), Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [TestCase("../evil")]
    [TestCase("..\\evil")]
    [TestCase("sub/dir.png")]
    [TestCase("")]
    public void WriteBlobAsync_WithUnsafeKey_Throws(string key) =>
        Assert.That(async () => await _sut.WriteBlobAsync("images", key, new byte[] { 1 }),
            Throws.InstanceOf<ArgumentException>());

    [TestCase("../secret")]
    [TestCase("a/b")]
    public void ReadBlobAsync_WithUnsafeKey_Throws(string key) =>
        Assert.That(async () => await _sut.ReadBlobAsync("images", key),
            Throws.InstanceOf<ArgumentException>());

    [TestCase("../secret")]
    public void DeleteBlobAsync_WithUnsafeKey_Throws(string key) =>
        Assert.That(async () => await _sut.DeleteBlobAsync("images", key),
            Throws.InstanceOf<ArgumentException>());

    [Test]
    public void IsAvailable_TrueWhenRootSet() => Assert.That(_sut.IsAvailable, Is.True);

    [Test]
    public void IsAvailable_FalseWhenRootBlank() =>
        Assert.That(new FileSystemSyncBackend("  ").IsAvailable, Is.False);
}
