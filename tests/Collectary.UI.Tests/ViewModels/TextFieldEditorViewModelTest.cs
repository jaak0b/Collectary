using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class TextFieldEditorViewModelTest
{
    [Test]
    public void LoadsAndPersists()
    {
        var sut = new TextFieldEditorViewModel(new TextFieldDefinition(), new TextFieldValue { Value = "hi" });
        Assert.That(sut.Text, Is.EqualTo("hi"));
        sut.Text = "bye";
        Assert.That(((TextFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo("bye"));
    }

    [Test]
    public void MaxLength_ReflectsDefinition()
    {
        var sut = new TextFieldEditorViewModel(new TextFieldDefinition { MaxLength = 40 }, new TextFieldValue());
        Assert.That(sut.MaxLength, Is.EqualTo(40));
    }

    [Test]
    public void MaxLength_IsZero_WhenUnset()
    {
        var sut = new TextFieldEditorViewModel(new TextFieldDefinition { MaxLength = null }, new TextFieldValue());
        Assert.That(sut.MaxLength, Is.EqualTo(0));
    }
}
