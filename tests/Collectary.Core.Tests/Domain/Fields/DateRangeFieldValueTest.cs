using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class DateRangeFieldValueTest
{
    [Test]
    public void IsEmpty_OnlyWhenBothNull()
    {
        Assert.That(new DateRangeFieldValue().IsEmpty, Is.True);
        Assert.That(new DateRangeFieldValue { From = new DateTime(2020, 1, 1) }.IsEmpty, Is.False);
        Assert.That(new DateRangeFieldValue { To = new DateTime(2020, 1, 1) }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_FormatsBothEnds()
    {
        var v = new DateRangeFieldValue { From = new DateTime(2018, 5, 1), To = new DateTime(2020, 6, 30) };
        Assert.That(v.ToString(), Is.EqualTo("2018-05-01 – 2020-06-30"));
    }

    [Test]
    public void ToString_MarksMissingEndWithQuestionMark()
    {
        var v = new DateRangeFieldValue { From = new DateTime(2018, 5, 1) };
        Assert.That(v.ToString(), Is.EqualTo("2018-05-01 – ?"));
    }

    [Test]
    public void ToString_EmptyWhenBothNull() =>
        Assert.That(new DateRangeFieldValue().ToString(), Is.EqualTo(""));

    [Test]
    public void CopyFrom_CopiesBothEnds()
    {
        var target = new DateRangeFieldValue();
        target.CopyFrom(new DateRangeFieldValue { From = new DateTime(2019, 1, 1), To = new DateTime(2019, 12, 31) });
        Assert.That(target.From, Is.EqualTo(new DateTime(2019, 1, 1)));
        Assert.That(target.To, Is.EqualTo(new DateTime(2019, 12, 31)));
    }
}
