using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class DurationListCellViewModelTest
{
    [Test]
    public void Display_UsesSourceToString()
    {
        var cell = new DurationListCellViewModel(
            new DurationFieldValue { TotalMinutes = 90 }, new DurationFieldDefinition());
        Assert.That(cell.Display, Is.EqualTo("1 h 30 min"));
    }

    [Test]
    public void Display_EmptyForBlankSource() =>
        Assert.That(new DurationListCellViewModel(new DurationFieldValue(), new DurationFieldDefinition()).Display, Is.EqualTo(""));
}
