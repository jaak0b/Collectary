using Collectary.Presentation.Services;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class CountryCatalogTest
{
    private readonly CountryCatalog _sut = new();

    [Test]
    public void ToFlag_MapsCodeToRegionalIndicators()
    {
        Assert.That(_sut.ToFlag("DE"), Is.EqualTo("🇩🇪"));
        Assert.That(_sut.ToFlag("us"), Is.EqualTo("🇺🇸"));
    }

    [Test]
    public void ToFlag_RejectsNonTwoLetterOrNonAlpha()
    {
        Assert.That(_sut.ToFlag("D"), Is.Empty);
        Assert.That(_sut.ToFlag("D1"), Is.Empty);
        Assert.That(_sut.ToFlag("1D"), Is.Empty);
        Assert.That(_sut.ToFlag("123"), Is.Empty);
    }

    [Test]
    public void Countries_IncludeKnownEntriesWithNamesAndFlags()
    {
        var catalog = new CountryCatalog();
        var germany = catalog.Find("DE");

        Assert.That(germany, Is.Not.Null);
        Assert.That(germany!.Name, Does.Contain("Germany"));
        Assert.That(germany.Display, Does.Contain("🇩🇪"));
        Assert.That(catalog.Countries, Is.Ordered.By(nameof(CountryOption.Name)));
    }

    [Test]
    public void Find_UnknownOrNull_ReturnsNull()
    {
        var catalog = new CountryCatalog();
        Assert.That(catalog.Find(null), Is.Null);
        Assert.That(catalog.Find("ZZ"), Is.Null);
    }
}
