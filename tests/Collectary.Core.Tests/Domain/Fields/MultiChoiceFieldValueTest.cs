using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class MultiChoiceFieldValueTest
{
    [Test]
    public void IsEmpty_WhenNoSelection()
    {
        Assert.That(new MultiChoiceFieldValue().IsEmpty, Is.True);
        Assert.That(new MultiChoiceFieldValue { Selected = { "a" } }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_JoinsWithComma()
    {
        Assert.That(new MultiChoiceFieldValue { Selected = { "a", "b" } }.ToString(), Is.EqualTo("a, b"));
        Assert.That(new MultiChoiceFieldValue().ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_ClonesSelectionIndependently()
    {
        var source = new MultiChoiceFieldValue { Selected = { "x" } };
        var target = new MultiChoiceFieldValue();
        target.CopyFrom(source);

        source.Selected.Add("y");
        Assert.That(target.Selected, Is.EqualTo(new[] { "x" }));
    }
}
