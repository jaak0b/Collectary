using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class ColorFieldValueTest
{
    [Test]
    public void IsEmpty_ForNullAndEmpty()
    {
        Assert.That(new ColorFieldValue { Value = null }.IsEmpty, Is.True);
        Assert.That(new ColorFieldValue { Value = "" }.IsEmpty, Is.True);
        Assert.That(new ColorFieldValue { Value = "#fff" }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsValueOrEmpty()
    {
        Assert.That(new ColorFieldValue { Value = "#fff" }.ToString(), Is.EqualTo("#fff"));
        Assert.That(new ColorFieldValue { Value = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesValue()
    {
        var target = new ColorFieldValue();
        target.CopyFrom(new ColorFieldValue { Value = "#abcdef" });
        Assert.That(target.Value, Is.EqualTo("#abcdef"));
    }
}
