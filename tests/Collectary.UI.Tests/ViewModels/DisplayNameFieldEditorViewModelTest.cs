using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class DisplayNameFieldEditorViewModelTest
{
    [Test]
    public void GetCurrentValue_Throws()
    {
        var sut = new DisplayNameFieldEditorViewModel(new DisplayNameFieldDefinition(), "Name");
        Assert.That(sut.Text, Is.EqualTo("Name"));
        Assert.Throws<NotSupportedException>(() => sut.GetCurrentValue());
    }
}
