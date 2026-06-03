using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class PercentageListCellViewModelTest
{
    [Test]
    public void Display_FormatsValueOrEmpty()
    {
        var withValue = new PercentageListCellViewModel(
            new PercentageFieldValue { Value = 42.5m }, new PercentageFieldDefinition());
        Assert.That(withValue.Display, Is.EqualTo($"{42.5m:F1} %"));

        var empty = new PercentageListCellViewModel(
            new PercentageFieldValue { Value = null }, new PercentageFieldDefinition());
        Assert.That(empty.Display, Is.EqualTo(""));
    }

    [Test]
    public void Display_EmptyForWrongValueType()
    {
        var cell = new PercentageListCellViewModel(new TextFieldValue { Value = "x" }, new PercentageFieldDefinition());
        Assert.That(cell.Display, Is.EqualTo(""));
    }
}
