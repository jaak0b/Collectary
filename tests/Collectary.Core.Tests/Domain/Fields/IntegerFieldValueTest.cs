using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class IntegerFieldValueTest
{
    [Test]
    public void IsEmpty_OnlyWhenNull()
    {
        Assert.That(new IntegerFieldValue { Value = null }.IsEmpty, Is.True);
        Assert.That(new IntegerFieldValue { Value = 0 }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsNumberOrEmpty()
    {
        Assert.That(new IntegerFieldValue { Value = 7 }.ToString(), Is.EqualTo("7"));
        Assert.That(new IntegerFieldValue { Value = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesValue()
    {
        var target = new IntegerFieldValue();
        target.CopyFrom(new IntegerFieldValue { Value = 42 });
        Assert.That(target.Value, Is.EqualTo(42));
    }
}
