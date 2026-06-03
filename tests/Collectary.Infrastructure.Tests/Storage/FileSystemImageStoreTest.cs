using Collectary.Infrastructure.Storage;

namespace Collectary.Infrastructure.Tests.Storage;

[TestFixture]
public class FileSystemImageStoreTest : FileSystemTestBase
{
    private FileSystemImageStore _sut = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _sut = new FileSystemImageStore(TempDir);
    }

    [Test]
    public async Task SaveAsync_ReturnKeyContainsFileName()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var key = await _sut.SaveAsync(stream, "photo.jpg");

        Assert.That(key, Does.EndWith("photo.jpg"));
    }

    [Test]
    public async Task SaveAsync_PersistsContentToTempDir()
    {
        var content = new byte[] { 10, 20, 30, 40 };
        using var stream = new MemoryStream(content);

        var key = await _sut.SaveAsync(stream, "file.bin");

        var savedPath = Path.Combine(TempDir, key);
        Assert.That(File.ReadAllBytes(savedPath), Is.EqualTo(content));
    }

    [Test]
    public void Exists_ReturnsFalse_WhenKeyNotFound()
    {
        Assert.That(_sut.Exists("nonexistent.jpg"), Is.False);
    }

    [Test]
    public async Task Exists_ReturnsTrue_AfterSave()
    {
        using var stream = new MemoryStream(new byte[] { 1 });
        var key = await _sut.SaveAsync(stream, "img.png");

        Assert.That(_sut.Exists(key), Is.True);
    }

    [Test]
    public async Task Open_ReturnsReadableStream()
    {
        var content = new byte[] { 5, 6, 7, 8 };
        using var writeStream = new MemoryStream(content);
        var key = await _sut.SaveAsync(writeStream, "data.bin");

        using var readStream = _sut.Open(key);
        var buffer = new byte[content.Length];
        _ = await readStream.ReadAsync(buffer);

        Assert.That(buffer, Is.EqualTo(content));
    }

    [Test]
    public async Task DeleteAsync_RemovesFile()
    {
        using var stream = new MemoryStream(new byte[] { 1 });
        var key = await _sut.SaveAsync(stream, "temp.png");

        await _sut.DeleteAsync(key);

        Assert.That(_sut.Exists(key), Is.False);
    }
}
