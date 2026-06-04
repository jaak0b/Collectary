using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class TextListCellViewModelTest
{
    [Test]
    public void Display_UsesSourceToString()
    {
        var cell = new TextListCellViewModel(new TextFieldValue { Value = "hi" }, new TextFieldDefinition());
        Assert.That(cell.Text, Is.EqualTo("hi"));
    }
}
