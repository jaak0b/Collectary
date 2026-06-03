using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class TimeFieldValueTest
{
    [Test]
    public void IsEmpty_ForWhitespace()
    {
        Assert.That(new TimeFieldValue { Value = " " }.IsEmpty, Is.True);
        Assert.That(new TimeFieldValue { Value = "12:00" }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsValueOrEmpty()
    {
        Assert.That(new TimeFieldValue { Value = "14:30" }.ToString(), Is.EqualTo("14:30"));
        Assert.That(new TimeFieldValue { Value = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesValue()
    {
        var target = new TimeFieldValue();
        target.CopyFrom(new TimeFieldValue { Value = "08:15" });
        Assert.That(target.Value, Is.EqualTo("08:15"));
    }
}
