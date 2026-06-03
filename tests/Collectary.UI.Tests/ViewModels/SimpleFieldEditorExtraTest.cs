using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SimpleFieldEditorExtraTest
{
    [Test]
    public void Bool_Constructor_LoadsTrueValue()
    {
        var sut = new BoolFieldEditorViewModel(new BoolFieldDefinition(), new BoolFieldValue { Value = true });
        Assert.That(sut.IsChecked, Is.True);
    }

    [Test]
    public void Bool_Constructor_NullValueDefaultsToFalse()
    {
        var sut = new BoolFieldEditorViewModel(new BoolFieldDefinition(), new BoolFieldValue { Value = null });
        Assert.That(sut.IsChecked, Is.False);
    }

    [Test]
    public void Bool_GetCurrentValue_ReflectsIsChecked()
    {
        var value = new BoolFieldValue();
        var sut = new BoolFieldEditorViewModel(new BoolFieldDefinition(), value) { IsChecked = true };

        Assert.That(((BoolFieldValue)sut.GetCurrentValue()).Value, Is.True);
    }

    [Test]
    public void Rating_Constructor_LoadsExistingStars()
    {
        var sut = new RatingFieldEditorViewModel(new RatingFieldDefinition(), new RatingFieldValue { Stars = 4 });
        Assert.That(sut.Stars, Is.EqualTo(4));
    }

    [Test]
    public void Rating_Constructor_NullStarsDefaultsToZero()
    {
        var sut = new RatingFieldEditorViewModel(new RatingFieldDefinition(), new RatingFieldValue { Stars = null });
        Assert.That(sut.Stars, Is.EqualTo(0));
    }

    [Test]
    public void Rating_GetCurrentValue_ZeroStoresNull()
    {
        var value = new RatingFieldValue();
        var sut = new RatingFieldEditorViewModel(new RatingFieldDefinition(), value) { Stars = 0 };

        Assert.That(((RatingFieldValue)sut.GetCurrentValue()).Stars, Is.Null);
    }

    [Test]
    public void Rating_GetCurrentValue_PositiveStoresValue()
    {
        var value = new RatingFieldValue();
        var sut = new RatingFieldEditorViewModel(new RatingFieldDefinition(), value) { Stars = 3 };

        Assert.That(((RatingFieldValue)sut.GetCurrentValue()).Stars, Is.EqualTo(3));
    }
}
