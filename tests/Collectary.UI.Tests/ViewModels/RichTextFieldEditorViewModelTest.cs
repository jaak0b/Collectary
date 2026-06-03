using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class RichTextFieldEditorViewModelTest
{
    [Test]
    public void LoadsAndPersists()
    {
        var sut = new RichTextFieldEditorViewModel(new RichTextFieldDefinition(), new RichTextFieldValue { Value = "# H" });
        Assert.That(sut.Markdown, Is.EqualTo("# H"));
        sut.Markdown = "## H2";
        Assert.That(((RichTextFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo("## H2"));
    }
}
