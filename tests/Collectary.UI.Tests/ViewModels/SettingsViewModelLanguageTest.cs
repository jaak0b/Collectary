using Collectary.UI.Localization;
using Collectary.UI.Services;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SettingsViewModelLanguageTest
{
    private string _originalPrefs = null!;
    private string _dir = null!;

    [SetUp]
    public void SetUp()
    {
        _originalPrefs = AppPreferences.FilePath;
        _dir = Path.Combine(Path.GetTempPath(), $"collectary-settings-{Guid.NewGuid():N}");
        AppPreferences.FilePath = Path.Combine(_dir, "preferences.json");
        LocalizationService.Instance.Apply("en");
    }

    [TearDown]
    public void TearDown()
    {
        AppPreferences.FilePath = _originalPrefs;
        LocalizationService.Instance.Apply("en");
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Test]
    public void SelectedLanguageChange_AppliesLocalization()
    {
        var sut = new SettingsViewModel(() => { });

        sut.SelectedLanguage = sut.LanguageOptions.Single(o => o.Code == "de");

        Assert.That(LocalizationService.Instance.CurrentCode, Is.EqualTo("de"));
    }

    [Test]
    public void SelectedLanguageChange_PersistsLanguageToPreferences()
    {
        var sut = new SettingsViewModel(() => { });

        sut.SelectedLanguage = sut.LanguageOptions.Single(o => o.Code == "de");

        Assert.That(AppPreferences.Load().Language, Is.EqualTo("de"));
    }

    [Test]
    public void SelectedLanguageSetToNull_DoesNotChangeCulture()
    {
        var sut = new SettingsViewModel(() => { });
        var before = LocalizationService.Instance.CurrentCode;

        sut.SelectedLanguage = null!;

        Assert.That(LocalizationService.Instance.CurrentCode, Is.EqualTo(before));
    }
}
