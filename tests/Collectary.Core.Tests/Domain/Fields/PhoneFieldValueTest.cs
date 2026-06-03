using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class PhoneFieldValueTest
{
    [Test]
    public void IsEmpty_ForWhitespace()
    {
        Assert.That(new PhoneFieldValue { Value = " " }.IsEmpty, Is.True);
        Assert.That(new PhoneFieldValue { Value = "555" }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsValueOrEmpty()
    {
        Assert.That(new PhoneFieldValue { Value = "555" }.ToString(), Is.EqualTo("555"));
        Assert.That(new PhoneFieldValue { Value = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesValue()
    {
        var target = new PhoneFieldValue();
        target.CopyFrom(new PhoneFieldValue { Value = "12345" });
        Assert.That(target.Value, Is.EqualTo("12345"));
    }
}
