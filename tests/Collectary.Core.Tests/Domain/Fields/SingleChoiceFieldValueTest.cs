using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class SingleChoiceFieldValueTest
{
    [Test]
    public void IsEmpty_ForNullAndEmpty()
    {
        Assert.That(new SingleChoiceFieldValue { Selected = null }.IsEmpty, Is.True);
        Assert.That(new SingleChoiceFieldValue { Selected = "" }.IsEmpty, Is.True);
        Assert.That(new SingleChoiceFieldValue { Selected = "a" }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_ReturnsSelectedOrEmpty()
    {
        Assert.That(new SingleChoiceFieldValue { Selected = "a" }.ToString(), Is.EqualTo("a"));
        Assert.That(new SingleChoiceFieldValue { Selected = null }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesSelection()
    {
        var target = new SingleChoiceFieldValue();
        target.CopyFrom(new SingleChoiceFieldValue { Selected = "x" });
        Assert.That(target.Selected, Is.EqualTo("x"));
    }
}
