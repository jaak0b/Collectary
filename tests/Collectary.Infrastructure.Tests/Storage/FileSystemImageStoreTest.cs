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
    public async Task SaveAsync_ReturnsGuidKeyWithExtension()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var key = await _sut.SaveAsync(stream, "photo.jpg");

        Assert.Multiple(() =>
        {
            Assert.That(key, Does.EndWith(".jpg"));
            Assert.That(key, Does.Not.Contain("photo"));
            Assert.That(key, Does.Not.Contain("_"));
            Assert.That(Path.GetFileNameWithoutExtension(key), Has.Length.EqualTo(32));
        });
    }

    [Test]
    public async Task SaveAsync_ExtensionlessFile_ReturnsBareGuidKey()
    {
        using var stream = new MemoryStream(new byte[] { 1 });

        var key = await _sut.SaveAsync(stream, "README");

        Assert.Multiple(() =>
        {
            Assert.That(key, Does.Not.Contain("."));
            Assert.That(key, Has.Length.EqualTo(32));
        });
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
    public async Task AudioWavBlob_RoundTripsAndDeletes()
    {
        var clip = new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4 };
        using var stream = new MemoryStream(clip);
        var key = await _sut.SaveAsync(stream, "audio-abc.wav");

        using (var read = _sut.Open(key))
        {
            var buffer = new byte[clip.Length];
            _ = await read.ReadAsync(buffer);
            Assert.That(buffer, Is.EqualTo(clip));
        }

        await _sut.DeleteAsync(key);
        Assert.That(_sut.Exists(key), Is.False);
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
