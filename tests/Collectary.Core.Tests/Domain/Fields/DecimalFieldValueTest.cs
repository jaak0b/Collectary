using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class DecimalFieldValueTest
{
    [Test]
    public void IsEmpty_OnlyWhenNull()
    {
        Assert.That(new DecimalFieldValue { Value = null }.IsEmpty, Is.True);
        Assert.That(new DecimalFieldValue { Value = 0m }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsNumberOrEmpty()
    {
        Assert.That(new DecimalFieldValue { Value = 1.25m }.ToString(), Is.EqualTo(1.25m.ToString()));
        Assert.That(new DecimalFieldValue { Value = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesValue()
    {
        var target = new DecimalFieldValue();
        target.CopyFrom(new DecimalFieldValue { Value = 3.5m });
        Assert.That(target.Value, Is.EqualTo(3.5m));
    }
}
