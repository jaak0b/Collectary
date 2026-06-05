using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class WeightFieldEditorViewModelTest
{
    [Test]
    public void LoadsAmountAndUnit()
    {
        var sut = new WeightFieldEditorViewModel(new WeightFieldDefinition(),
            new WeightFieldValue { Amount = 31.1m, Unit = "oz" });
        Assert.That(sut.Amount, Is.EqualTo(31.1m));
        Assert.That(sut.SelectedUnit, Is.EqualTo("oz"));
        Assert.That(sut.Units, Does.Contain("kg"));
    }

    [Test]
    public void GetCurrentValue_PersistsAmountAndUnit()
    {
        var sut = new WeightFieldEditorViewModel(new WeightFieldDefinition(), new WeightFieldValue());
        sut.Amount = 2m;
        sut.SelectedUnit = "lb";

        var v = (WeightFieldValue)sut.GetCurrentValue();
        Assert.That(v.Amount, Is.EqualTo(2m));
        Assert.That(v.Unit, Is.EqualTo("lb"));
    }
}
