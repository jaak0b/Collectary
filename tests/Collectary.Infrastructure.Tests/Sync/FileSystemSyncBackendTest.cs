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
        await _sut.WriteAsync("presets", id, "{\"hello\":1}");

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
        await _sut.WriteAsync("items", a, "A");
        await _sut.WriteAsync("items", b, "B");
        await _sut.WriteAsync("presets", Guid.NewGuid(), "X");

        var items = await _sut.ListAsync("items");

        Assert.That(items, Is.EquivalentTo(new[] { a, b }));
    }

    [Test]
    public async Task WriteAsync_ProducesTheFlatIdFileName()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("items", id, "A");

        Assert.That(File.Exists(Path.Combine(TempDir, "items", $"{id:N}.json")), Is.True,
            "the document layout is one flat {id}.json file per document");
    }

    [Test]
    public async Task WriteAsync_RemovesStaleFilesOfTheSameId()
    {
        var id = Guid.NewGuid();
        var dir = Path.Combine(TempDir, "items");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, $"{id:N}.5.json"), "leftover");

        await _sut.WriteAsync("items", id, "fresh");

        Assert.Multiple(() =>
        {
            Assert.That(Directory.EnumerateFiles(dir, "*.json").Count(), Is.EqualTo(1),
                "a write must leave exactly one file for the id, removing stale leftovers");
            Assert.That(_sut.ReadAsync("items", id).Result, Is.EqualTo("fresh"));
        });
    }

    [Test]
    public async Task ListAsync_IgnoresFilesThatAreNotFlatIdDocuments()
    {
        var id = Guid.NewGuid();
        var dir = Path.Combine(TempDir, "items");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, $"{id:N}.5.json"), "legacy");
        await File.WriteAllTextAsync(Path.Combine(dir, "notes.json"), "junk");

        Assert.That(await _sut.ListAsync("items"), Is.Empty,
            "only flat {id}.json documents are part of the sync layout");
    }

    [Test]
    public async Task DeleteAsync_MatchesDocumentRegardlessOfFilenameCase()
    {
        var id = Guid.NewGuid();
        var dir = Path.Combine(TempDir, "items");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, $"{id:N}".ToUpperInvariant() + ".json"), "x");

        await _sut.DeleteAsync("items", id);

        Assert.That(Directory.EnumerateFiles(dir, "*.json"), Is.Empty,
            "id matching must be case-insensitive so deletes work on case-sensitive filesystems");
    }

    [Test]
    public async Task ListAsync_WhenKindMissing_ReturnsEmpty() =>
        Assert.That(await _sut.ListAsync("nope"), Is.Empty);

    [Test]
    public async Task WriteAsync_Overwrites()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("presets", id, "first");
        await _sut.WriteAsync("presets", id, "second");

        Assert.That(await _sut.ReadAsync("presets", id), Is.EqualTo("second"));
    }

    [Test]
    public async Task DeleteAsync_RemovesDocument()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("presets", id, "x");

        await _sut.DeleteAsync("presets", id);

        Assert.That(await _sut.ReadAsync("presets", id), Is.Null);
    }

    [Test]
    public async Task WriteBlobAsync_ThenReadBlob_RoundTrips()
    {
        await _sut.WriteBlobAsync("images", "pic.png", new byte[] { 1, 2, 3 });

        Assert.That(await _sut.ReadBlobAsync("images", "pic.png"), Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public async Task DeleteBlobAsync_RemovesBlob()
    {
        await _sut.WriteBlobAsync("images", "a.png", new byte[] { 1 });

        await _sut.DeleteBlobAsync("images", "a.png");

        Assert.That(await _sut.ReadBlobAsync("images", "a.png"), Is.Null);
    }

    [Test]
    public async Task DeleteBlobAsync_WhenBlobMissing_DoesNothing()
    {
        await _sut.DeleteBlobAsync("images", "missing.png");

        Assert.That(await _sut.ListBlobKeysAsync("images"), Is.Empty);
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
