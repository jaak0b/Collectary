using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class PhoneFieldEditorViewModelTest
{
    [Test]
    public void LoadsAndPersists()
    {
        var sut = new PhoneFieldEditorViewModel(new PhoneFieldDefinition(), new PhoneFieldValue { Value = "555" });
        Assert.That(sut.Text, Is.EqualTo("555"));
        sut.Text = "999";
        Assert.That(((PhoneFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo("999"));
    }
}
