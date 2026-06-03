using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class DurationFieldValueTest
{
    [Test]
    public void IsEmpty_OnlyWhenNull()
    {
        Assert.That(new DurationFieldValue { TotalMinutes = null }.IsEmpty, Is.True);
        Assert.That(new DurationFieldValue { TotalMinutes = 0 }.IsEmpty, Is.False);
    }

    [TestCase(0, 0, "0 min")]
    [TestCase(0, 45, "45 min")]
    [TestCase(2, 15, "2 h 15 min")]
    [TestCase(1, 5, "1 h 05 min")]
    public void ToString_FormatsHoursAndMinutes(int hours, int minutes, string expected)
    {
        var value = new DurationFieldValue { TotalMinutes = hours * 60 + minutes };
        Assert.That(value.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void ToString_EmptyWhenNull() =>
        Assert.That(new DurationFieldValue { TotalMinutes = null }.ToString(), Is.EqualTo(""));

    [Test]
    public void CopyFrom_CopiesMinutes()
    {
        var target = new DurationFieldValue();
        target.CopyFrom(new DurationFieldValue { TotalMinutes = 90 });
        Assert.That(target.TotalMinutes, Is.EqualTo(90));
    }
}
