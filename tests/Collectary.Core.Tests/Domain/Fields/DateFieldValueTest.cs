using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class DateFieldValueTest
{
    [Test]
    public void IsEmpty_OnlyWhenNull()
    {
        Assert.That(new DateFieldValue { Value = null }.IsEmpty, Is.True);
        Assert.That(new DateFieldValue { Value = DateTime.UtcNow }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_UsesShortDateOrEmpty()
    {
        var date = new DateTime(2024, 1, 15);
        Assert.That(new DateFieldValue { Value = date }.ToString(), Is.EqualTo(date.ToString("d")));
        Assert.That(new DateFieldValue { Value = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesValue()
    {
        var date = new DateTime(2024, 5, 1);
        var target = new DateFieldValue();
        target.CopyFrom(new DateFieldValue { Value = date });
        Assert.That(target.Value, Is.EqualTo(date));
    }
}
