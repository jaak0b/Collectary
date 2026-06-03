using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class RichTextListCellViewModelTest
{
    [Test]
    public void Preview_StripsMarkdownAndCollapsesWhitespace()
    {
        var cell = new RichTextListCellViewModel(
            new RichTextFieldValue { Value = "# Title\n\n**bold**   text" }, new RichTextFieldDefinition());
        Assert.That(cell.Preview, Is.EqualTo("Title bold text"));
    }

    [Test]
    public void Preview_EmptyForBlank()
    {
        var cell = new RichTextListCellViewModel(
            new RichTextFieldValue { Value = "   " }, new RichTextFieldDefinition());
        Assert.That(cell.Preview, Is.EqualTo(""));
    }

    [Test]
    public void Preview_TruncatesLongText()
    {
        var longText = new string('a', 200);
        var cell = new RichTextListCellViewModel(
            new RichTextFieldValue { Value = longText }, new RichTextFieldDefinition());
        Assert.That(cell.Preview, Has.Length.EqualTo(81));
        Assert.That(cell.Preview, Does.EndWith("…"));
    }
}
