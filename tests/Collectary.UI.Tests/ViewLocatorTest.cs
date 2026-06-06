using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.SharedFields;

namespace Collectary.UI.Tests;

[TestFixture]
public class ViewLocatorTest
{
    private readonly System.Reflection.Assembly _uiAssembly = typeof(Collectary.UI.ViewLocator).Assembly;

    private static string MapToViewTypeName(Type viewModelType) =>
        viewModelType.FullName!
            .Replace("Collectary.Presentation", "Collectary.UI")
            .Replace("ViewModel", "View");

    [Test]
    public void TopLevelViewModel_MapsToResolvableView()
    {
        var name = MapToViewTypeName(typeof(HomeViewModel));

        Assert.That(name, Is.EqualTo("Collectary.UI.Views.HomeView"));
        Assert.That(_uiAssembly.GetType(name), Is.Not.Null,
            "A top-level ViewModel must map to an existing View type");
    }

    [Test]
    public void WelcomeViewModel_MapsToResolvableView()
    {
        var name = MapToViewTypeName(typeof(WelcomeViewModel));

        Assert.That(name, Is.EqualTo("Collectary.UI.Views.WelcomeView"));
        Assert.That(_uiAssembly.GetType(name), Is.Not.Null,
            "A top-level ViewModel must map to an existing View type");
    }

    [Test]
    public void PresetTemplatePickerViewModel_MapsToResolvableView()
    {
        var name = MapToViewTypeName(typeof(PresetTemplatePickerViewModel));

        Assert.That(name, Is.EqualTo("Collectary.UI.Views.PresetTemplatePickerView"));
        Assert.That(_uiAssembly.GetType(name), Is.Not.Null,
            "The template picker ViewModel must map to an existing View type");
    }

    [Test]
    public void SubNamespacedViewModel_MapsToViewInMatchingSubNamespace()
    {
        var name = MapToViewTypeName(typeof(SharedFieldLibraryViewModel));

        Assert.That(name, Is.EqualTo("Collectary.UI.Views.SharedFields.SharedFieldLibraryView"));
        Assert.That(_uiAssembly.GetType(name), Is.Not.Null,
            "A ViewModel in a sub-namespace must map to its View in the matching UI sub-namespace");
    }
}
