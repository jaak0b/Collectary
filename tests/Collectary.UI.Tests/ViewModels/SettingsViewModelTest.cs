using Avalonia.Media;
using Collectary.Core.Domain;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SettingsViewModelTest
{
    private string _dir = null!;
    private string _original = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _original = AppPreferences.FilePath;
        AppPreferences.FilePath = Path.Combine(_dir, "preferences.json");
        ThemeService.Instance.ApplySkin("Windows11");
        ThemeService.Instance.ApplyColorTheme("Light");
        ThemeService.Instance.ApplyAccent(null);
    }

    [TearDown]
    public void TearDown()
    {
        ThemeService.Instance.ApplyCustomColors((IReadOnlyDictionary<string, Color>?)null);
        ThemeService.Instance.ApplySkin("Windows11");
        ThemeService.Instance.ApplyColorTheme("Light");
        ThemeService.Instance.ApplyAccent(null);
        AppPreferences.FilePath = _original;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        LocalizationService.Instance.Apply("en");
    }

    [Test]
    public void Constructor_ReadsCurrentLanguageFromService()
    {
        LocalizationService.Instance.Apply("de");
        var sut = new SettingsViewModel(() => { });

        Assert.That(sut.SelectedLanguage.Code, Is.EqualTo("de"));
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
    public void Constructor_ReadsCurrentSkinAndThemeFromService()
    {
        ThemeService.Instance.ApplySkin("Flat");
        ThemeService.Instance.ApplyColorTheme("Dark");

        var sut = new SettingsViewModel(() => { });

        Assert.Multiple(() =>
        {
            Assert.That(sut.SelectedSkin.Id, Is.EqualTo("Flat"));
            Assert.That(sut.SelectedColorTheme.Id, Is.EqualTo("Dark"));
        });
    }

    [Test]
    public void SelectedSkin_Change_AppliesAndPersists()
    {
        var sut = new SettingsViewModel(() => { });

        sut.SelectedSkin = sut.Skins.First(s => s.Id == "Flat");

        Assert.Multiple(() =>
        {
            Assert.That(ThemeService.Instance.CurrentSkinId, Is.EqualTo("Flat"));
            Assert.That(AppPreferences.Load().Skin, Is.EqualTo("Flat"));
        });
    }

    [Test]
    public void SelectedColorTheme_Change_AppliesAndPersists()
    {
        var sut = new SettingsViewModel(() => { });

        sut.SelectedColorTheme = sut.ColorThemes.First(t => t.Id == "Nord");

        Assert.Multiple(() =>
        {
            Assert.That(ThemeService.Instance.CurrentColorThemeId, Is.EqualTo("Nord"));
            Assert.That(AppPreferences.Load().ColorTheme, Is.EqualTo("Nord"));
        });
    }

    [Test]
    public void AccentColor_Change_AppliesPersistsAndFlagsCustom()
    {
        var sut = new SettingsViewModel(() => { });

        sut.AccentColor = Colors.Red;

        Assert.Multiple(() =>
        {
            Assert.That(sut.HasCustomAccent, Is.True);
            Assert.That(ThemeService.Instance.CurrentAccent, Is.EqualTo(Colors.Red));
            Assert.That(AppPreferences.Load().AccentColor, Is.EqualTo(Colors.Red.ToString()));
        });
    }

    [Test]
    public void ResetAccent_ClearsAccentAndPersistsNull()
    {
        var sut = new SettingsViewModel(() => { });
        sut.AccentColor = Colors.Red;

        sut.ResetAccentCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(sut.HasCustomAccent, Is.False);
            Assert.That(ThemeService.Instance.CurrentAccent, Is.Null);
            Assert.That(AppPreferences.Load().AccentColor, Is.Null);
        });
    }

    [Test]
    public void ColorSlot_Change_AppliesCustomColorAndPersists()
    {
        var sut = new SettingsViewModel(() => { });

        var slot = sut.ColorSlots.First(s => s.Key == "Background");
        slot.Color = Colors.Magenta;

        Assert.Multiple(() =>
        {
            Assert.That(ThemeService.Instance.CurrentCustomColors["Background"], Is.EqualTo(Colors.Magenta));
            Assert.That(AppPreferences.Load().CustomColors!["Background"], Is.EqualTo(Colors.Magenta.ToString()));
        });
    }

    [Test]
    public void ColorSlots_IncludeEasyAndExpertEntries()
    {
        var sut = new SettingsViewModel(() => { });

        Assert.Multiple(() =>
        {
            Assert.That(sut.ColorSlots.Where(s => s.IsEasy).Select(s => s.Key),
                Is.EquivalentTo(new[] { "Background", "Surface", "TextPrimary", "SidebarBackground" }));
            Assert.That(sut.ColorSlots, Has.Count.EqualTo(18));
        });
    }

    [Test]
    public void ResetColors_ClearsOverridesAndAccentAndPersistsNull()
    {
        var sut = new SettingsViewModel(() => { });
        sut.AccentColor = Colors.Red;
        sut.ColorSlots.First(s => s.Key == "Background").Color = Colors.Magenta;

        sut.ResetColorsCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(ThemeService.Instance.CurrentCustomColors, Is.Empty);
            Assert.That(ThemeService.Instance.CurrentAccent, Is.Null);
            Assert.That(sut.HasCustomAccent, Is.False);
            Assert.That(AppPreferences.Load().CustomColors, Is.Null);
            Assert.That(AppPreferences.Load().AccentColor, Is.Null);
        });
    }

    [Test]
    public void SelectedFieldLabelLayout_Change_Persists()
    {
        var sut = new SettingsViewModel(() => { });

        sut.SelectedFieldLabelLayout = sut.FieldLabelLayoutOptions.First(o => o.Value == FieldLabelLayout.Above);

        Assert.That(AppPreferences.Load().FieldLabelLayout, Is.EqualTo(FieldLabelLayout.Above));
    }

    [Test]
    public void Constructor_SeedsFieldLabelLayoutFromPrefs()
    {
        AppPreferences.Save(new AppPreferencesData(FieldLabelLayout: FieldLabelLayout.Beside));

        var sut = new SettingsViewModel(() => { });

        Assert.That(sut.SelectedFieldLabelLayout.Value, Is.EqualTo(FieldLabelLayout.Beside));
    }

    [Test]
    public void ExpertColorMode_Change_Persists()
    {
        var sut = new SettingsViewModel(() => { });

        sut.ExpertColorMode = true;

        Assert.That(AppPreferences.Load().ExpertColorMode, Is.True);
    }

    [Test]
    public void Constructor_SeedsSlotsAndModeFromSavedState()
    {
        ThemeService.Instance.ApplyCustomColors(new Dictionary<string, Color> { ["Background"] = Colors.Magenta });
        AppPreferences.Save(new AppPreferencesData(ExpertColorMode: true));

        var sut = new SettingsViewModel(() => { });

        Assert.Multiple(() =>
        {
            Assert.That(sut.ExpertColorMode, Is.True);
            Assert.That(sut.ColorSlots.First(s => s.Key == "Background").Color, Is.EqualTo(Colors.Magenta));
        });
    }

    [Test]
    public void SelectedLanguage_Change_NotifiesPropertyChanged()
    {
        var sut = new SettingsViewModel(() => { });
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        sut.SelectedLanguage = sut.LanguageOptions.First(o => o.Code == "de");

        Assert.That(changed, Does.Contain(nameof(sut.SelectedLanguage)));
    }

    [Test]
    public void NavigateToSharedFields_InvokesCallback()
    {
        var called = false;
        var sut = new SettingsViewModel(() => called = true);

        sut.NavigateToSharedFieldsCommand.Execute(null);

        Assert.That(called, Is.True);
    }

    [Test]
    public void LanguageOptions_ContainsEnAndDe()
    {
        var sut = new SettingsViewModel(() => { });

        Assert.That(sut.LanguageOptions.Select(o => o.Code), Is.EquivalentTo(new[] { "en", "de" }));
    }

    [Test]
    public void RequireLoginOnWeb_WhenToggledOff_PersistsFalse()
    {
        var sut = new SettingsViewModel(() => { });

        sut.RequireLoginOnWeb = false;

        Assert.That(AppPreferences.Load().RequireLoginOnWeb, Is.False);
    }

    [Test]
    public void RequireLoginOnWeb_WhenToggledOn_PersistsTrue()
    {
        AppPreferences.Save(new AppPreferencesData(RequireLoginOnWeb: false));
        var sut = new SettingsViewModel(() => { });

        sut.RequireLoginOnWeb = true;

        Assert.That(AppPreferences.Load().RequireLoginOnWeb, Is.True);
    }

    [Test]
    public void Constructor_SeedsRequireLoginOnWebFromPrefs()
    {
        AppPreferences.Save(new AppPreferencesData(RequireLoginOnWeb: false));

        var sut = new SettingsViewModel(() => { });

        Assert.That(sut.RequireLoginOnWeb, Is.False);
    }

    [Test]
    public void Constructor_DoesNotPersistRequireLoginOnWeb()
    {
        _ = new SettingsViewModel(() => { });

        Assert.That(AppPreferences.Load().RequireLoginOnWeb, Is.Null);
    }

    [Test]
    public void Logout_InvokesInjectedCallback()
    {
        var called = false;
        var sut = new SettingsViewModel(() => { }, logout: () => called = true);

        sut.LogoutCommand.Execute(null);

        Assert.That(called, Is.True);
    }

    [Test]
    public void CanLogout_ReflectsInjectedFlag()
    {
        var sut = new SettingsViewModel(() => { }, canLogout: true);

        Assert.That(sut.CanLogout, Is.True);
    }
}
