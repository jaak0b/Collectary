using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class TimeListCellViewModelTest
{
    [Test]
    public void Display_UsesSourceToString()
    {
        var cell = new TimeListCellViewModel(new TimeFieldValue { Value = "08:15" }, new TimeFieldDefinition());
        Assert.That(cell.Display, Is.EqualTo("08:15"));
    }

    [Test]
    public void Display_EmptyForBlankSource() =>
        Assert.That(new TimeListCellViewModel(new TimeFieldValue(), new TimeFieldDefinition()).Display, Is.EqualTo(""));
}
