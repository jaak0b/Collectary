using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class BoolFieldValueTest
{
    [Test]
    public void IsEmpty_OnlyWhenNull()
    {
        Assert.That(new BoolFieldValue { Value = null }.IsEmpty, Is.True);
        Assert.That(new BoolFieldValue { Value = false }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_YesNoOrEmpty()
    {
        Assert.That(new BoolFieldValue { Value = true }.ToString(), Is.EqualTo("Yes"));
        Assert.That(new BoolFieldValue { Value = false }.ToString(), Is.EqualTo("No"));
        Assert.That(new BoolFieldValue { Value = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesValue()
    {
        var target = new BoolFieldValue();
        target.CopyFrom(new BoolFieldValue { Value = true });
        Assert.That(target.Value, Is.True);
    }
}
