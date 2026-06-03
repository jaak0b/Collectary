using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class CurrencyFieldValueTest
{
    [Test]
    public void IsEmpty_OnlyWhenNull()
    {
        Assert.That(new CurrencyFieldValue { Value = null }.IsEmpty, Is.True);
        Assert.That(new CurrencyFieldValue { Value = 0m }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_TwoDecimalsOrEmpty()
    {
        Assert.That(new CurrencyFieldValue { Value = 29.9m }.ToString(), Is.EqualTo($"{29.9m:F2}"));
        Assert.That(new CurrencyFieldValue { Value = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesValue()
    {
        var target = new CurrencyFieldValue();
        target.CopyFrom(new CurrencyFieldValue { Value = 9.99m });
        Assert.That(target.Value, Is.EqualTo(9.99m));
    }
}
