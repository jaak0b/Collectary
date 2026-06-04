using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class TagsListCellViewModelTest
{
    [Test]
    public void Display_UsesSourceToString()
    {
        var cell = new TagsListCellViewModel(
            new TagsFieldValue { Tags = { "a", "b" } }, new TagsFieldDefinition());
        Assert.That(cell.Display, Is.EqualTo("a, b"));
    }

    [Test]
    public void Display_EmptyForBlankSource() =>
        Assert.That(new TagsListCellViewModel(new TagsFieldValue(), new TagsFieldDefinition()).Display, Is.EqualTo(""));
}
