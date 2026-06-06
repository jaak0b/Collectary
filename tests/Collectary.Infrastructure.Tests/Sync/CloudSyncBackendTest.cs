using Collectary.Infrastructure.Sync;
using Collectary.Infrastructure.Tests.Infrastructure;

namespace Collectary.Infrastructure.Tests.Sync;

[TestFixture]
public class CloudSyncBackendTest
{
    private FakeCloudFileStore _fileStore = null!;
    private CloudSyncBackend _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fileStore = new FakeCloudFileStore();
        _sut = new CloudSyncBackend(_fileStore);
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
    public async Task ReadAsync_WithMultipleRevisionsPresent_ReturnsHighest()
    {
        var id = Guid.NewGuid();
        await _sut.ListAsync("items"); // provisions the kind folder
        var folder = (await _fileStore.ListFoldersAsync("root", CancellationToken.None)).Single(f => f.Name == "items");
        var naming = new SyncFileNaming();
        await _fileStore.UploadAsync(folder.Id, naming.DocumentName(id, 1), System.Text.Encoding.UTF8.GetBytes("rev1"), CancellationToken.None);
        await _fileStore.UploadAsync(folder.Id, naming.DocumentName(id, 2), System.Text.Encoding.UTF8.GetBytes("rev2"), CancellationToken.None);

        Assert.That(await _sut.ReadAsync("items", id), Is.EqualTo("rev2"),
            "a lingering lower revision must never shadow the newest one");
    }

    [Test]
    public async Task ListAsync_WithMultipleRevisionsPresent_CollapsesToHighest()
    {
        var id = Guid.NewGuid();
        await _sut.ListAsync("items");
        var folder = (await _fileStore.ListFoldersAsync("root", CancellationToken.None)).Single(f => f.Name == "items");
        var naming = new SyncFileNaming();
        await _fileStore.UploadAsync(folder.Id, naming.DocumentName(id, 1), System.Text.Encoding.UTF8.GetBytes("rev1"), CancellationToken.None);
        await _fileStore.UploadAsync(folder.Id, naming.DocumentName(id, 2), System.Text.Encoding.UTF8.GetBytes("rev2"), CancellationToken.None);

        var entries = await _sut.ListAsync("items");

        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Count.EqualTo(1), "two revisions of one id must collapse to a single entry");
            Assert.That(entries.Single().Revision, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task WriteAsync_DoesNotDeleteAHigherConcurrentRevision()
    {
        var id = Guid.NewGuid();
        await _sut.ListAsync("items");
        var folder = (await _fileStore.ListFoldersAsync("root", CancellationToken.None)).Single(f => f.Name == "items");
        var naming = new SyncFileNaming();
        // a concurrent writer has already landed revision 3
        await _fileStore.UploadAsync(folder.Id, naming.DocumentName(id, 3), System.Text.Encoding.UTF8.GetBytes("rev3"), CancellationToken.None);

        await _sut.WriteAsync("items", id, "rev2", 2);

        Assert.That(await _sut.ReadAsync("items", id), Is.EqualTo("rev3"),
            "an in-flight older write must not prune a newer concurrent revision");
    }

    [Test]
    public async Task ReadAndWrite_AreScopedToTheirOwnId()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await _sut.WriteAsync("items", a, "A1", 1);
        await _sut.WriteAsync("items", b, "B5", 5);   // a different id at a higher revision
        await _sut.WriteAsync("items", a, "A2", 2);    // rewriting A must not read or prune B

        Assert.Multiple(() =>
        {
            Assert.That(_sut.ReadAsync("items", a).Result, Is.EqualTo("A2"), "a read must never pick another id's revision");
            Assert.That(_sut.ReadAsync("items", b).Result, Is.EqualTo("B5"), "another id's document must survive a write");
        });
    }

    [Test]
    public async Task ListAsync_WhenKindEmpty_ReturnsEmpty() =>
        Assert.That(await _sut.ListAsync("presets"), Is.Empty);

    [Test]
    public async Task WriteAsync_SameRevision_Overwrites()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("presets", id, "first", 1);
        await _sut.WriteAsync("presets", id, "second", 1);

        Assert.That(await _sut.ReadAsync("presets", id), Is.EqualTo("second"));
    }

    [Test]
    public async Task DeleteAsync_RemovesAllRevisionsOfId()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("presets", id, "x", 1);

        await _sut.DeleteAsync("presets", id);

        Assert.Multiple(() =>
        {
            Assert.That(_sut.ReadAsync("presets", id).Result, Is.Null);
            Assert.That(_sut.ListAsync("presets").Result, Is.Empty);
        });
    }

    [Test]
    public async Task WriteBlobAsync_ThenReadBlob_RoundTrips()
    {
        await _sut.WriteBlobAsync("images", "pic.png", new byte[] { 1, 2, 3 });

        Assert.That(await _sut.ReadBlobAsync("images", "pic.png"), Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public async Task ListBlobKeysAsync_ReturnsKeys()
    {
        await _sut.WriteBlobAsync("images", "a.png", new byte[] { 1 });
        await _sut.WriteBlobAsync("images", "b.png", new byte[] { 2 });

        Assert.That(await _sut.ListBlobKeysAsync("images"), Is.EquivalentTo(new[] { "a.png", "b.png" }));
    }

    [Test]
    public async Task DeleteBlobAsync_RemovesBlob()
    {
        await _sut.WriteBlobAsync("images", "a.png", new byte[] { 1 });

        await _sut.DeleteBlobAsync("images", "a.png");

        Assert.That(await _sut.ReadBlobAsync("images", "a.png"), Is.Null);
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
    public void IsAvailable_TrueWhenStoreAvailable() => Assert.That(_sut.IsAvailable, Is.True);

    [Test]
    public void IsAvailable_FalseWhenStoreUnavailable()
    {
        _fileStore.IsAvailable = false;
        Assert.That(_sut.IsAvailable, Is.False);
    }

    [Test]
    public async Task Invalidate_ForcesKindFolderReresolution()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("items", id, "a", 1);
        _sut.Invalidate();
        await _sut.WriteAsync("items", id, "b", 2);

        Assert.Multiple(() =>
        {
            Assert.That(_fileStore.EnsureFolderCalls, Is.EqualTo(2),
                "after invalidation the kind folder must be resolved again, not served from the stale cache");
            Assert.That(_fileStore.InvalidateCalls, Is.EqualTo(1),
                "invalidation must propagate to the underlying file store (e.g. to reset the OneDrive drive id)");
        });
    }

    [Test]
    public async Task KindFolder_WhenRootFolderChanges_ResolvesUnderTheNewRoot()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("items", id, "first", 1);

        _fileStore.RootFolderId = "root2";
        await _sut.WriteAsync("items", id, "second", 2);

        var newFolder = (await _fileStore.ListFoldersAsync("root2", CancellationToken.None)).SingleOrDefault(f => f.Name == "items");
        Assert.That(newFolder, Is.Not.Null, "a kind folder must be re-resolved under the newly chosen root, not served from the stale cache");
        Assert.That(await _fileStore.DownloadAsync(newFolder!.Id, $"{id:N}.2.json", CancellationToken.None), Is.Not.Null,
            "documents must be written under the new root after the sync folder changed");
    }

    [Test]
    public async Task KindFolder_ResolvedOncePerKind_AndCached()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("items", id, "a", 1);
        await _sut.ListAsync("items");
        await _sut.ReadAsync("items", id);

        Assert.That(_fileStore.EnsureFolderCalls, Is.EqualTo(1));
    }
}
