using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class ImageFieldValueTest
{
    [Test]
    public void IsEmpty_BasedOnImageKey()
    {
        Assert.That(new ImageFieldValue { ImageKey = null }.IsEmpty, Is.True);
        Assert.That(new ImageFieldValue { ImageKey = "" }.IsEmpty, Is.True);
        Assert.That(new ImageFieldValue { ImageKey = "k" }.IsEmpty, Is.False);
    }

    [Test]
    public void CopyFrom_CopiesKeyAndFileName()
    {
        var target = new ImageFieldValue();
        target.CopyFrom(new ImageFieldValue { ImageKey = "k", FileName = "f.png" });
        Assert.That(target.ImageKey, Is.EqualTo("k"));
        Assert.That(target.FileName, Is.EqualTo("f.png"));
    }
}
