using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class RichTextFieldValueTest
{
    [Test]
    public void IsEmpty_ForWhitespace()
    {
        Assert.That(new RichTextFieldValue { Value = " \n " }.IsEmpty, Is.True);
        Assert.That(new RichTextFieldValue { Value = "# Title" }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsValueOrEmpty()
    {
        Assert.That(new RichTextFieldValue { Value = "# Title" }.ToString(), Is.EqualTo("# Title"));
        Assert.That(new RichTextFieldValue { Value = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesValue()
    {
        var target = new RichTextFieldValue();
        target.CopyFrom(new RichTextFieldValue { Value = "**x**" });
        Assert.That(target.Value, Is.EqualTo("**x**"));
    }
}
