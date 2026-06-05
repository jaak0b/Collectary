using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class MeasurementFieldEditorViewModelTest
{
    [Test]
    public void LoadsAmountAndUnit()
    {
        var sut = new MeasurementFieldEditorViewModel(new MeasurementFieldDefinition(),
            new MeasurementFieldValue { Amount = 38m, Unit = "cm" });
        Assert.That(sut.Amount, Is.EqualTo(38m));
        Assert.That(sut.SelectedUnit, Is.EqualTo("cm"));
        Assert.That(sut.Units, Does.Contain("mm"));
    }

    [Test]
    public void GetCurrentValue_PersistsAmountAndUnit()
    {
        var sut = new MeasurementFieldEditorViewModel(new MeasurementFieldDefinition(), new MeasurementFieldValue());
        sut.Amount = 4.5m;
        sut.SelectedUnit = "in";

        var v = (MeasurementFieldValue)sut.GetCurrentValue();
        Assert.That(v.Amount, Is.EqualTo(4.5m));
        Assert.That(v.Unit, Is.EqualTo("in"));
    }
}
