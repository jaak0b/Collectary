using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class MultiImageFieldValueTest
{
    private static MultiImagePicture Pic(string key, string name) => new(key, name);

    [Test]
    public void IsEmpty_WhenNoPictures()
    {
        Assert.That(new MultiImageFieldValue().IsEmpty, Is.True);
        Assert.That(new MultiImageFieldValue { Pictures = [Pic("a", "a.jpg")] }.IsEmpty, Is.False);
    }

    [Test]
    public void CopyFrom_CopiesPicturesAsIndependentList()
    {
        var source = new MultiImageFieldValue { Pictures = [Pic("a", "a.jpg"), Pic("b", "b.jpg")] };
        var target = new MultiImageFieldValue();

        target.CopyFrom(source);
        source.Pictures.Add(Pic("c", "c.jpg"));

        Assert.That(target.Pictures.Select(p => p.Key), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void ToString_ReportsCount()
    {
        Assert.That(new MultiImageFieldValue { Pictures = [Pic("a", "a.jpg"), Pic("b", "b.jpg")] }.ToString(), Is.EqualTo("2"));
        Assert.That(new MultiImageFieldValue().ToString(), Is.EqualTo("0"));
    }

    [Test]
    public void ReferencedBlobKeys_ReturnsAllKeys()
    {
        Assert.That(new MultiImageFieldValue { Pictures = [Pic("a", "a.jpg"), Pic("b", "b.jpg")] }.ReferencedBlobKeys(),
            Is.EqualTo(new[] { "a", "b" }));
        Assert.That(new MultiImageFieldValue().ReferencedBlobKeys(), Is.Empty);
    }

    [Test]
    public void ImageKeys_Getter_MirrorsPictureKeys()
    {
        var value = new MultiImageFieldValue { Pictures = [Pic("g1_a.jpg", "a.jpg"), Pic("g2_b.png", "b.png")] };

        Assert.That(value.ImageKeys, Is.EqualTo(new[] { "g1_a.jpg", "g2_b.png" }));
    }

    [Test]
    public void ImageKeys_Setter_UsesKeyAsFileName()
    {
        var value = new MultiImageFieldValue { ImageKeys = ["abc-123_photo.jpg"] };

        Assert.That(value.Pictures.Single().Key, Is.EqualTo("abc-123_photo.jpg"));
        Assert.That(value.Pictures.Single().FileName, Is.EqualTo("abc-123_photo.jpg"));
    }

    [Test]
    public void ImageKeys_Setter_DoesNotOverwriteExistingPictures()
    {
        var value = new MultiImageFieldValue { Pictures = [Pic("g_real.jpg", "ORIGINAL-NAME.jpg")] };

        value.ImageKeys = ["g_real.jpg"];

        Assert.That(value.Pictures.Single().FileName, Is.EqualTo("ORIGINAL-NAME.jpg"));
    }
}
