using Collectary.Presentation.Localization;

namespace Collectary.UI.Tests;

[TestFixture]
public class LocalizationServiceTest
{
    [TearDown]
    public void Reset() => LocalizationService.Instance.Apply("en");

    [Test]
    public void Indexer_ReturnsEnglishString()
    {
        LocalizationService.Instance.Apply("en");
        Assert.That(LocalizationService.Instance["Save"], Is.EqualTo("Save"));
    }

    [Test]
    public void Apply_German_SwitchesStringsAndCode()
    {
        LocalizationService.Instance.Apply("de");
        Assert.That(LocalizationService.Instance.CurrentCode, Is.EqualTo("de"));
        Assert.That(LocalizationService.Instance["Save"], Is.EqualTo("Speichern"));
    }

    [Test]
    public void Indexer_FallsBackToKey_WhenMissing() =>
        Assert.That(LocalizationService.Instance["___no_such_key___"], Is.EqualTo("___no_such_key___"));

    [Test]
    public void Apply_RaisesLanguageChangedAndPropertyChanged()
    {
        var languageChanged = false;
        var propertyChanged = false;
        LocalizationService.Instance.LanguageChanged += Handler;
        LocalizationService.Instance.PropertyChanged += PropHandler;
        try
        {
            LocalizationService.Instance.Apply("de");
            Assert.That(languageChanged, Is.True);
            Assert.That(propertyChanged, Is.True);
        }
        finally
        {
            LocalizationService.Instance.LanguageChanged -= Handler;
            LocalizationService.Instance.PropertyChanged -= PropHandler;
        }

        void Handler(object? s, EventArgs e) => languageChanged = true;
        void PropHandler(object? s, System.ComponentModel.PropertyChangedEventArgs e) => propertyChanged = true;
    }
}
