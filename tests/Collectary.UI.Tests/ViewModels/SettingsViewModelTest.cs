using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SettingsViewModelTest
{
    [Test]
    public void Constructor_ReadsCurrentLanguageFromService()
    {
        LocalizationService.Instance.Apply("de");
        var sut = new SettingsViewModel(() => { });

        Assert.That(sut.SelectedLanguage.Code, Is.EqualTo("de"));

        LocalizationService.Instance.Apply("en");
    }

    [Test]
    public void Constructor_DefaultsToEnglishWhenCodeUnknown()
    {
        LocalizationService.Instance.Apply("en");
        var sut = new SettingsViewModel(() => { });

        Assert.That(sut.SelectedLanguage, Is.Not.Null);
        Assert.That(sut.SelectedLanguage.Code, Is.EqualTo("en"));
    }

    [Test]
    public void CurrentTheme_Light_IsLightThemeTrueIsDarkFalse()
    {
        var sut = new SettingsViewModel(() => { }) { CurrentTheme = AppTheme.Light };

        Assert.That(sut.IsLightTheme, Is.True);
        Assert.That(sut.IsDarkTheme, Is.False);
    }

    [Test]
    public void CurrentTheme_Dark_IsDarkThemeTrueIsLightFalse()
    {
        var sut = new SettingsViewModel(() => { }) { CurrentTheme = AppTheme.Dark };

        Assert.That(sut.IsDarkTheme, Is.True);
        Assert.That(sut.IsLightTheme, Is.False);
    }

    [Test]
    public void CurrentTheme_Change_NotifiesIsLightAndIsDark()
    {
        var sut = new SettingsViewModel(() => { }) { CurrentTheme = AppTheme.Light };
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.CurrentTheme = AppTheme.Dark;

        Assert.That(changed, Does.Contain(nameof(sut.IsLightTheme)));
        Assert.That(changed, Does.Contain(nameof(sut.IsDarkTheme)));
    }

    [Test]
    public void SelectedLanguage_Change_NotifiesPropertyChanged()
    {
        var sut = new SettingsViewModel(() => { });
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.SelectedLanguage = sut.LanguageOptions.First(o => o.Code == "de");

        Assert.That(changed, Does.Contain(nameof(sut.SelectedLanguage)));

        LocalizationService.Instance.Apply("en");
    }

    [Test]
    public void NavigateToSystemFields_InvokesCallback()
    {
        var called = false;
        var sut = new SettingsViewModel(() => called = true);

        sut.NavigateToSystemFieldsCommand.Execute(null);

        Assert.That(called, Is.True);
    }

    [Test]
    public void LanguageOptions_ContainsEnAndDe()
    {
        var sut = new SettingsViewModel(() => { });

        Assert.That(sut.LanguageOptions.Select(o => o.Code), Is.EquivalentTo(new[] { "en", "de" }));
    }

    [Test]
    public void SetThemeCommand_WithSameThemeAsCurrentTheme_DoesNotChangeTheme()
    {
        var sut = new SettingsViewModel(() => { }) { CurrentTheme = AppTheme.Light };
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.SetThemeCommand.Execute("Light");

        Assert.That(changed, Does.Not.Contain(nameof(sut.CurrentTheme)));
    }
}
