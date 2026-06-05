using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SliderFieldEditorViewModelTest
{
    [Test]
    public void LoadsValue()
    {
        var sut = new SliderFieldEditorViewModel(new SliderFieldDefinition(), new SliderFieldValue { Value = 60 });
        Assert.That(sut.Number, Is.EqualTo(60));
        Assert.That(sut.Minimum, Is.EqualTo(0));
        Assert.That(sut.Maximum, Is.EqualTo(100));
    }

    [Test]
    public void NullValue_DefaultsToZero()
    {
        var sut = new SliderFieldEditorViewModel(new SliderFieldDefinition(), new SliderFieldValue());
        Assert.That(sut.Number, Is.EqualTo(0));
    }

    [Test]
    public void GetCurrentValue_PersistsRoundedInt()
    {
        var sut = new SliderFieldEditorViewModel(new SliderFieldDefinition(), new SliderFieldValue());
        sut.Number = 73.6;
        Assert.That(((SliderFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo(74));
    }
}
