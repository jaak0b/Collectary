using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class EmailFieldEditorViewModelTest
{
    [Test]
    public void LoadsAndPersists()
    {
        var sut = new EmailFieldEditorViewModel(new EmailFieldDefinition(), new EmailFieldValue { Value = "a@b" });
        Assert.That(sut.Text, Is.EqualTo("a@b"));
        sut.Text = "c@d";
        Assert.That(((EmailFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo("c@d"));
    }
}
