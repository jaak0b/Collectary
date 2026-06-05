using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class CountryFieldEditorViewModelTest
{
    private readonly ICountryCatalog _catalog = new CountryCatalog();

    [Test]
    public void LoadsSelectedCountryFromCode()
    {
        var sut = new CountryFieldEditorViewModel(new CountryFieldDefinition(),
            new CountryFieldValue { Code = "DE" }, _catalog);

        Assert.That(sut.SelectedCountry, Is.Not.Null);
        Assert.That(sut.SelectedCountry!.Code, Is.EqualTo("DE"));
        Assert.That(sut.SelectedCountry.Flag, Is.EqualTo("🇩🇪"));
    }

    [Test]
    public void NullCode_LeavesNoSelection()
    {
        var sut = new CountryFieldEditorViewModel(new CountryFieldDefinition(),
            new CountryFieldValue(), _catalog);
        Assert.That(sut.SelectedCountry, Is.Null);
        Assert.That(sut.Countries, Is.Not.Empty);
    }

    [Test]
    public void GetCurrentValue_PersistsSelectedCode()
    {
        var sut = new CountryFieldEditorViewModel(new CountryFieldDefinition(),
            new CountryFieldValue(), _catalog);

        sut.SelectedCountry = _catalog.Find("FR");

        Assert.That(((CountryFieldValue)sut.GetCurrentValue()).Code, Is.EqualTo("FR"));
    }

    [Test]
    public void ClearingSelection_PersistsNull()
    {
        var sut = new CountryFieldEditorViewModel(new CountryFieldDefinition(),
            new CountryFieldValue { Code = "US" }, _catalog);

        sut.SelectedCountry = null;

        Assert.That(((CountryFieldValue)sut.GetCurrentValue()).Code, Is.Null);
    }
}
