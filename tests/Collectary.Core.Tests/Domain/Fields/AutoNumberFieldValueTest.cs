using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class AutoNumberFieldValueTest
{
    [Test]
    public void IsEmpty_OnlyWhenNull()
    {
        Assert.That(new AutoNumberFieldValue { Value = null }.IsEmpty, Is.True);
        Assert.That(new AutoNumberFieldValue { Value = 0 }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsNumberOrEmpty()
    {
        Assert.That(new AutoNumberFieldValue { Value = 7 }.ToString(), Is.EqualTo("7"));
        Assert.That(new AutoNumberFieldValue { Value = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesValue()
    {
        var target = new AutoNumberFieldValue();
        target.CopyFrom(new AutoNumberFieldValue { Value = 42 });
        Assert.That(target.Value, Is.EqualTo(42));
    }
}
