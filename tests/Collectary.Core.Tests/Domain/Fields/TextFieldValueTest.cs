using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class TextFieldValueTest
{
    [Test]
    public void IsEmpty_ForNullWhitespaceAndValue()
    {
        Assert.That(new TextFieldValue { Value = null }.IsEmpty, Is.True);
        Assert.That(new TextFieldValue { Value = "   " }.IsEmpty, Is.True);
        Assert.That(new TextFieldValue { Value = "x" }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsValueOrEmpty()
    {
        Assert.That(new TextFieldValue { Value = "hi" }.ToString(), Is.EqualTo("hi"));
        Assert.That(new TextFieldValue { Value = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesMatchingType()
    {
        var target = new TextFieldValue { Value = "old" };
        target.CopyFrom(new TextFieldValue { Value = "new" });
        Assert.That(target.Value, Is.EqualTo("new"));
    }

    [Test]
    public void CopyFrom_IgnoresMismatchedType()
    {
        var target = new TextFieldValue { Value = "keep" };
        target.CopyFrom(new IntegerFieldValue { Value = 5 });
        Assert.That(target.Value, Is.EqualTo("keep"));
    }
}
