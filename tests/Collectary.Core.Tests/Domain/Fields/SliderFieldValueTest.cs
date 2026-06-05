using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class SliderFieldValueTest
{
    [Test]
    public void IsEmpty_WhenNull()
    {
        Assert.That(new SliderFieldValue().IsEmpty, Is.True);
        Assert.That(new SliderFieldValue { Value = 0 }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsValueOrEmpty()
    {
        Assert.That(new SliderFieldValue { Value = 75 }.ToString(), Is.EqualTo("75"));
        Assert.That(new SliderFieldValue().ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesValue()
    {
        var target = new SliderFieldValue();
        target.CopyFrom(new SliderFieldValue { Value = 42 });
        Assert.That(target.Value, Is.EqualTo(42));
    }
}
