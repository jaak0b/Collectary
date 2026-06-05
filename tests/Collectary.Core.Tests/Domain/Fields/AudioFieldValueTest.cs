using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class AudioFieldValueTest
{
    [Test]
    public void IsEmpty_WhenNoKey()
    {
        Assert.That(new AudioFieldValue().IsEmpty, Is.True);
        Assert.That(new AudioFieldValue { AudioKey = "a" }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ShowsDurationOrEmpty()
    {
        Assert.That(new AudioFieldValue { AudioKey = "a", DurationSeconds = 12 }.ToString(), Is.EqualTo("12s"));
        Assert.That(new AudioFieldValue().ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesKeyAndDuration()
    {
        var target = new AudioFieldValue();
        target.CopyFrom(new AudioFieldValue { AudioKey = "k", DurationSeconds = 5 });
        Assert.That(target.AudioKey, Is.EqualTo("k"));
        Assert.That(target.DurationSeconds, Is.EqualTo(5));
    }

    [Test]
    public void ReferencedBlobKeys_ReturnsAudioKeyOrNothing()
    {
        Assert.That(new AudioFieldValue { AudioKey = "k" }.ReferencedBlobKeys(), Is.EqualTo(new[] { "k" }));
        Assert.That(new AudioFieldValue().ReferencedBlobKeys(), Is.Empty);
    }
}
