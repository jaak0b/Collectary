using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class DecimalFieldEditorViewModelTest
{
    [Test]
    public void LoadsAndPersists()
    {
        var sut = new DecimalFieldEditorViewModel(new DecimalFieldDefinition(), new DecimalFieldValue { Value = 1.5m });
        Assert.That(sut.Number, Is.EqualTo(1.5m));
        sut.Number = 2.25m;
        Assert.That(((DecimalFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo(2.25m));
    }

    [Test]
    public void FormatString_ReflectsDecimalPlaces()
    {
        var sut = new DecimalFieldEditorViewModel(new DecimalFieldDefinition { DecimalPlaces = 3 }, new DecimalFieldValue());
        Assert.That(sut.FormatString, Is.EqualTo("0.000"));
    }

    [Test]
    public void FormatString_IsIntegerPattern_WhenZeroPlaces()
    {
        var sut = new DecimalFieldEditorViewModel(new DecimalFieldDefinition { DecimalPlaces = 0 }, new DecimalFieldValue());
        Assert.That(sut.FormatString, Is.EqualTo("0"));
    }
}
