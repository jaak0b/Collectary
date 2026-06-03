using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class PercentageFieldValueTest
{
    [Test]
    public void IsEmpty_OnlyWhenNull()
    {
        Assert.That(new PercentageFieldValue { Value = null }.IsEmpty, Is.True);
        Assert.That(new PercentageFieldValue { Value = 0m }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_OneDecimalWithSignOrEmpty()
    {
        Assert.That(new PercentageFieldValue { Value = 42.5m }.ToString(), Is.EqualTo($"{42.5m:F1} %"));
        Assert.That(new PercentageFieldValue { Value = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesValue()
    {
        var target = new PercentageFieldValue();
        target.CopyFrom(new PercentageFieldValue { Value = 12.3m });
        Assert.That(target.Value, Is.EqualTo(12.3m));
    }
}
