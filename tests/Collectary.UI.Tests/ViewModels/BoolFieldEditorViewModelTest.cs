using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class BoolFieldEditorViewModelTest
{
    [Test]
    public void TwoState_LoadsFalseFromNull_AndPersists()
    {
        var sut = new BoolFieldEditorViewModel(new BoolFieldDefinition(), new BoolFieldValue { Value = null });
        Assert.That(sut.IsChecked, Is.False);
        sut.IsChecked = true;
        Assert.That(((BoolFieldValue)sut.GetCurrentValue()).Value, Is.True);
    }

    [Test]
    public void TwoState_UntouchedNull_PersistsAsFalse()
    {
        var sut = new BoolFieldEditorViewModel(new BoolFieldDefinition { ThreeState = false }, new BoolFieldValue { Value = null });
        Assert.That(((BoolFieldValue)sut.GetCurrentValue()).Value, Is.False);
    }

    [Test]
    public void ThreeState_UntouchedNull_StaysNull()
    {
        var sut = new BoolFieldEditorViewModel(new BoolFieldDefinition { ThreeState = true }, new BoolFieldValue { Value = null });
        Assert.That(sut.IsChecked, Is.Null);
        Assert.That(((BoolFieldValue)sut.GetCurrentValue()).Value, Is.Null);
    }

    [Test]
    public void ThreeState_PersistsExplicitFalse()
    {
        var sut = new BoolFieldEditorViewModel(new BoolFieldDefinition { ThreeState = true }, new BoolFieldValue { Value = null });
        sut.IsChecked = false;
        Assert.That(((BoolFieldValue)sut.GetCurrentValue()).Value, Is.False);
    }

    [Test]
    public void IsThreeState_ReflectsDefinition()
    {
        Assert.That(new BoolFieldEditorViewModel(new BoolFieldDefinition { ThreeState = true }, new BoolFieldValue()).IsThreeState, Is.True);
        Assert.That(new BoolFieldEditorViewModel(new BoolFieldDefinition { ThreeState = false }, new BoolFieldValue()).IsThreeState, Is.False);
    }
}
