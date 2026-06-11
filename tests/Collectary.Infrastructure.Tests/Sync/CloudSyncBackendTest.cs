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
        await _sut.WriteAsync("presets", id, "{\"hello\":1}");

        Assert.That(await _sut.ReadAsync("presets", id), Is.EqualTo("{\"hello\":1}"));
    }

    [Test]
    public async Task ReadAsync_WhenMissing_ReturnsNull() =>
        Assert.That(await _sut.ReadAsync("presets", Guid.NewGuid()), Is.Null);

    [Test]
    public async Task ReadAsync_DownloadsDirectlyWithoutListingTheFolder()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("items", id, "hi");
        var listsBefore = _fileStore.ListFilesCalls;

        var content = await _sut.ReadAsync("items", id);

        Assert.Multiple(() =>
        {
            Assert.That(content, Is.EqualTo("hi"));
            Assert.That(_fileStore.ListFilesCalls - listsBefore, Is.EqualTo(0),
                "a read addresses the flat document name directly, it never re-lists the folder");
        });
    }

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
    public async Task WriteAsync_UploadsTheFlatIdFileName()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("items", id, "A");

        var folder = (await _fileStore.ListFoldersAsync("root", CancellationToken.None)).Single(f => f.Name == "items");
        Assert.That(await _fileStore.DownloadAsync(folder.Id, $"{id:N}.json", CancellationToken.None), Is.Not.Null,
            "the document layout is one flat {id}.json file per document");
    }

    [Test]
    public async Task WriteAsync_RemovesStaleFilesOfTheSameId()
    {
        var id = Guid.NewGuid();
        await _sut.ListAsync("items");
        var folder = (await _fileStore.ListFoldersAsync("root", CancellationToken.None)).Single(f => f.Name == "items");
        await _fileStore.UploadAsync(folder.Id, $"{id:N}.5.json", System.Text.Encoding.UTF8.GetBytes("leftover"), CancellationToken.None);

        await _sut.WriteAsync("items", id, "fresh");

        var remaining = (await _fileStore.ListFilesAsync(folder.Id, CancellationToken.None)).Select(f => f.Name).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(remaining, Is.EqualTo(new[] { $"{id:N}.json" }),
                "a write must leave exactly one file for the id, removing stale leftovers");
            Assert.That(_sut.ReadAsync("items", id).Result, Is.EqualTo("fresh"));
        });
    }

    [Test]
    public async Task ListAsync_IgnoresFilesThatAreNotFlatIdDocuments()
    {
        var id = Guid.NewGuid();
        await _sut.ListAsync("items");
        var folder = (await _fileStore.ListFoldersAsync("root", CancellationToken.None)).Single(f => f.Name == "items");
        await _fileStore.UploadAsync(folder.Id, $"{id:N}.5.json", System.Text.Encoding.UTF8.GetBytes("legacy"), CancellationToken.None);
        await _fileStore.UploadAsync(folder.Id, "notes.json", System.Text.Encoding.UTF8.GetBytes("junk"), CancellationToken.None);

        Assert.That(await _sut.ListAsync("items"), Is.Empty,
            "only flat {id}.json documents are part of the sync layout");
    }

    [Test]
    public async Task ReadAndWrite_AreScopedToTheirOwnId()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await _sut.WriteAsync("items", a, "A1");
        await _sut.WriteAsync("items", b, "B5");
        await _sut.WriteAsync("items", a, "A2");

        Assert.Multiple(() =>
        {
            Assert.That(_sut.ReadAsync("items", a).Result, Is.EqualTo("A2"), "a read must never pick another id's document");
            Assert.That(_sut.ReadAsync("items", b).Result, Is.EqualTo("B5"), "another id's document must survive a write");
        });
    }

    [Test]
    public async Task ListAsync_WhenKindEmpty_ReturnsEmpty() =>
        Assert.That(await _sut.ListAsync("presets"), Is.Empty);

    [Test]
    public async Task WriteAsync_Overwrites()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("presets", id, "first");
        await _sut.WriteAsync("presets", id, "second");

        Assert.That(await _sut.ReadAsync("presets", id), Is.EqualTo("second"));
    }

    [Test]
    public async Task DeleteAsync_RemovesAllFilesOfId()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("presets", id, "x");

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
        await _sut.WriteAsync("items", id, "a");
        _sut.Invalidate();
        await _sut.WriteAsync("items", id, "b");

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
        await _sut.WriteAsync("items", id, "first");

        _fileStore.RootFolderId = "root2";
        await _sut.WriteAsync("items", id, "second");

        var newFolder = (await _fileStore.ListFoldersAsync("root2", CancellationToken.None)).SingleOrDefault(f => f.Name == "items");
        Assert.That(newFolder, Is.Not.Null, "a kind folder must be re-resolved under the newly chosen root, not served from the stale cache");
        Assert.That(await _fileStore.DownloadAsync(newFolder!.Id, $"{id:N}.json", CancellationToken.None), Is.Not.Null,
            "documents must be written under the new root after the sync folder changed");
    }

    [Test]
    public async Task KindFolder_ResolvedOncePerKind_AndCached()
    {
        var id = Guid.NewGuid();
        await _sut.WriteAsync("items", id, "a");
        await _sut.ListAsync("items");
        await _sut.ReadAsync("items", id);

        Assert.That(_fileStore.EnsureFolderCalls, Is.EqualTo(1));
    }
}
