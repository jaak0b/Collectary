using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class EmailFieldValueTest
{
    [Test]
    public void IsEmpty_ForWhitespace()
    {
        Assert.That(new EmailFieldValue { Value = " " }.IsEmpty, Is.True);
        Assert.That(new EmailFieldValue { Value = "a@b" }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsValueOrEmpty()
    {
        Assert.That(new EmailFieldValue { Value = "a@b" }.ToString(), Is.EqualTo("a@b"));
        Assert.That(new EmailFieldValue { Value = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesValue()
    {
        var target = new EmailFieldValue();
        target.CopyFrom(new EmailFieldValue { Value = "c@d" });
        Assert.That(target.Value, Is.EqualTo("c@d"));
    }
}
