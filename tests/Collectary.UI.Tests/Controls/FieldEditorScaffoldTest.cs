using Collectary.UI.Controls;

namespace Collectary.UI.Tests.Controls;

[TestFixture]
public class FieldEditorScaffoldTest
{
    [Test]
    public void LabelAbove_True_AddsAbovePseudoClass()
    {
        var scaffold = new FieldEditorScaffold { LabelAbove = true };

        Assert.That(scaffold.Classes, Does.Contain(":above"));
    }

    [Test]
    public void LabelAbove_False_HasNoAbovePseudoClass()
    {
        var scaffold = new FieldEditorScaffold();

        Assert.That(scaffold.Classes, Does.Not.Contain(":above"));
    }

    [Test]
    public void LabelAbove_TogglingOff_RemovesAbovePseudoClass()
    {
        var scaffold = new FieldEditorScaffold { LabelAbove = true };

        scaffold.LabelAbove = false;

        Assert.That(scaffold.Classes, Does.Not.Contain(":above"));
    }

    [Test]
    public void Label_And_IsRequired_AreSettable()
    {
        var scaffold = new FieldEditorScaffold { Label = "Name", IsRequired = true };

        Assert.Multiple(() =>
        {
            Assert.That(scaffold.Label, Is.EqualTo("Name"));
            Assert.That(scaffold.IsRequired, Is.True);
        });
    }
}
