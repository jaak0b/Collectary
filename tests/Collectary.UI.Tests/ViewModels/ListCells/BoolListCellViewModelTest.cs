using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels.ListCells;

namespace Collectary.UI.Tests.ViewModels.ListCells;

[TestFixture]
public class BoolListCellViewModelTest
{
    [TearDown]
    public void Reset() => LocalizationService.Instance.Apply("en");

    [Test]
    public void Display_True_IsLocalizedYes()
    {
        LocalizationService.Instance.Apply("en");
        var sut = new BoolListCellViewModel(new BoolFieldValue { Value = true }, new BoolFieldDefinition());
        Assert.That(sut.Display, Is.EqualTo("Yes"));
    }

    [Test]
    public void Display_False_IsLocalizedNo()
    {
        LocalizationService.Instance.Apply("en");
        var sut = new BoolListCellViewModel(new BoolFieldValue { Value = false }, new BoolFieldDefinition());
        Assert.That(sut.Display, Is.EqualTo("No"));
    }

    [Test]
    public void Display_True_IsGermanWhenLanguageGerman()
    {
        LocalizationService.Instance.Apply("de");
        var sut = new BoolListCellViewModel(new BoolFieldValue { Value = true }, new BoolFieldDefinition());
        Assert.That(sut.Display, Is.EqualTo("Ja"));
    }

    [Test]
    public void Display_Empty_WhenValueNull()
    {
        var sut = new BoolListCellViewModel(new BoolFieldValue { Value = null }, new BoolFieldDefinition());
        Assert.That(sut.Display, Is.Empty);
    }
}
