using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class ProgressFieldValueTest
{
    [Test]
    public void IsEmpty_OnlyWhenBothNull()
    {
        Assert.That(new ProgressFieldValue().IsEmpty, Is.True);
        Assert.That(new ProgressFieldValue { Total = 100 }.IsEmpty, Is.False);
        Assert.That(new ProgressFieldValue { Have = 3 }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ShowsHaveOverTotal()
    {
        Assert.That(new ProgressFieldValue { Have = 42, Total = 100 }.ToString(), Is.EqualTo("42/100"));
        Assert.That(new ProgressFieldValue().ToString(), Is.EqualTo("0/0"));
    }

    [Test]
    public void CopyFrom_CopiesHaveAndTotal()
    {
        var target = new ProgressFieldValue();
        target.CopyFrom(new ProgressFieldValue { Have = 5, Total = 12 });
        Assert.That(target.Have, Is.EqualTo(5));
        Assert.That(target.Total, Is.EqualTo(12));
    }
}
