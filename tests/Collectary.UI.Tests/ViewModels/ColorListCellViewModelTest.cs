using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ColorListCellViewModelTest
{
    [Test]
    public void SwatchHex_ProducesArgbForValidColor()
    {
        var def = new ColorFieldDefinition { Format = ColorFormat.Hex };
        var cell = new ColorListCellViewModel(new ColorFieldValue { Value = "#FF0000" }, def);
        Assert.That(cell.SwatchHex, Is.EqualTo("#FFFF0000"));
    }

    [Test]
    public void SwatchHex_TransparentForInvalidValue()
    {
        var def = new ColorFieldDefinition { Format = ColorFormat.Hex };
        var cell = new ColorListCellViewModel(new ColorFieldValue { Value = null }, def);
        Assert.That(cell.SwatchHex, Is.EqualTo("#00FFFFFF"));
    }
}
