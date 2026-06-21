using System.Text;
using Collectary.UI.Storage;

namespace Collectary.UI.Tests;

[TestFixture]
public class InMemoryImageStoreTest
{
    private InMemoryImageStore _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new InMemoryImageStore();

    private static Stream Bytes(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Test]
    public async Task SaveAsync_ThenOpen_RoundTripsContentAndReturnsGuidKeyWithExtension()
    {
        var key = await _sut.SaveAsync(Bytes("hello"), "pic.png");

        using var reader = new StreamReader(_sut.Open(key));
        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadToEnd(), Is.EqualTo("hello"));
            Assert.That(key, Does.EndWith(".png"));
            Assert.That(key, Does.Not.Contain("pic"));
            Assert.That(key, Does.Not.Contain("_"));
            Assert.That(_sut.Exists(key), Is.True);
        });
    }

    [Test]
    public void Open_UnknownKey_Throws() =>
        Assert.Throws<FileNotFoundException>(() => _sut.Open("missing"));

    [Test]
    public async Task DeleteAsync_RemovesImage()
    {
        var key = await _sut.SaveAsync(Bytes("data"), "a.png");

        await _sut.DeleteAsync(key);

        Assert.That(_sut.Exists(key), Is.False);
    }

    [Test]
    public async Task ImportAsync_StoresUnderGivenKey_AndIsListed()
    {
        await _sut.ImportAsync("known-key", Bytes("imported"));

        var keys = await _sut.ListKeysAsync();
        Assert.Multiple(() =>
        {
            Assert.That(keys, Does.Contain("known-key"));
            using var reader = new StreamReader(_sut.Open("known-key"));
            Assert.That(reader.ReadToEnd(), Is.EqualTo("imported"));
        });
    }

    [Test]
    public void Exists_UnknownKey_False() => Assert.That(_sut.Exists("nope"), Is.False);
}
