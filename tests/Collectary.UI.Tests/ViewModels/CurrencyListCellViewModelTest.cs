using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class CurrencyListCellViewModelTest
{
    [Test]
    public void Display_PrependsSymbolOrEmpty()
    {
        var def = new CurrencyFieldDefinition { CurrencySymbol = "$" };
        var withValue = new CurrencyListCellViewModel(new CurrencyFieldValue { Value = 9.5m }, def);
        Assert.That(withValue.Display, Is.EqualTo($"$ {9.5m:F2}"));

        var empty = new CurrencyListCellViewModel(new CurrencyFieldValue { Value = null }, def);
        Assert.That(empty.Display, Is.EqualTo(""));
    }
}
