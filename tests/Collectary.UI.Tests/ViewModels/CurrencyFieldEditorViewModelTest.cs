using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class CurrencyFieldEditorViewModelTest
{
    [Test]
    public void LoadsPersistsAndExposesSymbol()
    {
        var def = new CurrencyFieldDefinition { CurrencySymbol = "$" };
        var sut = new CurrencyFieldEditorViewModel(def, new CurrencyFieldValue { Value = 9.99m });
        Assert.That(sut.Amount, Is.EqualTo(9.99m));
        Assert.That(sut.CurrencySymbol, Is.EqualTo("$"));
        sut.Amount = 12m;
        Assert.That(((CurrencyFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo(12m));
    }
}
