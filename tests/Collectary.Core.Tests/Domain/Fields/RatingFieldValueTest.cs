using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class RatingFieldValueTest
{
    [Test]
    public void IsEmpty_OnlyWhenNull()
    {
        Assert.That(new RatingFieldValue { Stars = null }.IsEmpty, Is.True);
        Assert.That(new RatingFieldValue { Stars = 0 }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_RendersStars()
    {
        Assert.That(new RatingFieldValue { Stars = 3 }.ToString(), Is.EqualTo("★★★"));
        Assert.That(new RatingFieldValue { Stars = 0 }.ToString(), Is.EqualTo(""));
        Assert.That(new RatingFieldValue { Stars = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesStars()
    {
        var target = new RatingFieldValue();
        target.CopyFrom(new RatingFieldValue { Stars = 4 });
        Assert.That(target.Stars, Is.EqualTo(4));
    }
}
