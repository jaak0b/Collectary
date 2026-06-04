using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class UrlFieldEditorViewModelTest
{
    [Test]
    public void LoadsAndPersists()
    {
        var sut = new UrlFieldEditorViewModel(new UrlFieldDefinition(), new UrlFieldValue { Url = "http://a" });
        Assert.That(sut.Url, Is.EqualTo("http://a"));
        sut.Url = "http://b";
        Assert.That(((UrlFieldValue)sut.GetCurrentValue()).Url, Is.EqualTo("http://b"));
    }
}
