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
    public async Task DeleteProfileCommand_InvokesTheCallback()
    {
        var invoked = false;
        var sut = new SettingsViewModel(() => { }, deleteProfile: () => { invoked = true; return Task.CompletedTask; });

        await sut.DeleteProfileCommand.ExecuteAsync(null);

        Assert.That(invoked, Is.True);
    }

    [Test]
    public void DeleteProfileCommand_WithNoCallback_DoesNotThrow()
    {
        var sut = new SettingsViewModel(() => { });

        Assert.That(async () => await sut.DeleteProfileCommand.ExecuteAsync(null), Throws.Nothing);
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
    public void SavingAppearance_NeutralizesStaleLegacyThemeField_SoExplicitColorThemeWins()
    {
        AppPreferences.Save(new AppPreferencesData(Theme: AppTheme.Dark, ColorTheme: "Dark"));
        ThemeService.Instance.ApplyColorTheme("Dark");
        var sut = new SettingsViewModel(() => { });

        sut.SelectedColorTheme = sut.ColorThemes.First(t => t.Id == "Light");

        var saved = AppPreferences.Load();
        Assert.Multiple(() =>
        {
            Assert.That(saved.ColorTheme, Is.EqualTo("Light"));
            Assert.That(saved.EffectiveColorTheme(), Is.EqualTo("Light"),
                "a stale legacy Theme=Dark must not override an explicitly chosen Light color theme on next boot");
        });
    }

    [Test]
    public void SelectedColorTheme_Change_RefreshesAccentSwatchToNewThemePrimary()
    {
        var sut = new SettingsViewModel(() => { });
        var lightPrimary = sut.AccentColor;

        sut.SelectedColorTheme = sut.ColorThemes.First(t => t.Id == "Dark");

        Assert.Multiple(() =>
        {
            Assert.That(sut.AccentColor, Is.EqualTo(Color.Parse("#60A5FA")),
                "the accent swatch must follow the newly selected theme's primary colour");
            Assert.That(sut.AccentColor, Is.Not.EqualTo(lightPrimary));
        });
    }

    [Test]
    public void Customizing_AColorSlot_FlagsCustomizationWithBasedOnLabel()
    {
        LocalizationService.Instance.Apply("en");
        var sut = new SettingsViewModel(() => { });

        sut.ColorSlots.First(s => s.Key == "Background").Color = Colors.Magenta;

        Assert.Multiple(() =>
        {
            Assert.That(sut.HasCustomizations, Is.True);
            Assert.That(sut.CustomThemeLabel, Does.Contain("Light"),
                "the badge names the built-in theme the customization is based on");
        });
    }

    [Test]
    public void Constructor_SeedsHasCustomizations_FromSavedCustomColors()
    {
        ThemeService.Instance.ApplyCustomColors(new Dictionary<string, Color> { ["Background"] = Colors.Magenta });

        var sut = new SettingsViewModel(() => { });

        Assert.That(sut.HasCustomizations, Is.True);
    }

    [Test]
    public void SwitchingBaseTheme_WithCustomizations_OnConfirm_ClearsColorsAccentAndApplies()
    {
        var sut = new SettingsViewModel(() => { }, confirmDiscardCustomizations: () => Task.FromResult(true));
        sut.ColorSlots.First(s => s.Key == "Background").Color = Colors.Magenta;
        sut.AccentColor = Colors.Red;
        Assume.That(sut.HasCustomizations, Is.True);

        sut.SelectedColorTheme = sut.ColorThemes.First(t => t.Id == "Dark");

        Assert.Multiple(() =>
        {
            Assert.That(ThemeService.Instance.CurrentColorThemeId, Is.EqualTo("Dark"));
            Assert.That(ThemeService.Instance.CurrentCustomColors, Is.Empty);
            Assert.That(ThemeService.Instance.CurrentAccent, Is.Null, "switching base theme must clear the custom accent");
            Assert.That(sut.HasCustomAccent, Is.False);
            Assert.That(sut.HasCustomizations, Is.False);
            Assert.That(sut.SelectedColorTheme.Id, Is.EqualTo("Dark"));
            Assert.That(AppPreferences.Load().CustomColors, Is.Null);
            Assert.That(AppPreferences.Load().AccentColor, Is.Null);
        });
    }

    [Test]
    public void CustomThemeLabel_UsesTheBaseThemesDisplayName_NotItsId()
    {
        LocalizationService.Instance.Apply("en");
        ThemeService.Instance.ApplyColorTheme("SolarizedLight");
        var sut = new SettingsViewModel(() => { });

        sut.ColorSlots.First(s => s.Key == "Background").Color = Colors.Magenta;

        Assert.That(sut.CustomThemeLabel, Does.Contain("Solarized Light"),
            "the badge shows the human display name (\"Solarized Light\"), not the id (\"SolarizedLight\")");
    }

    [Test]
    public void SwitchingBaseTheme_WithCustomizations_OnCancel_KeepsCustomizationsAndReverts()
    {
        ThemeService.Instance.ApplyColorTheme("Light");
        var sut = new SettingsViewModel(() => { }, confirmDiscardCustomizations: () => Task.FromResult(false));
        sut.ColorSlots.First(s => s.Key == "Background").Color = Colors.Magenta;

        sut.SelectedColorTheme = sut.ColorThemes.First(t => t.Id == "Dark");

        Assert.Multiple(() =>
        {
            Assert.That(ThemeService.Instance.CurrentColorThemeId, Is.EqualTo("Light"),
                "cancelling the confirm must not switch the theme");
            Assert.That(ThemeService.Instance.CurrentCustomColors["Background"], Is.EqualTo(Colors.Magenta));
            Assert.That(sut.HasCustomizations, Is.True);
            Assert.That(sut.SelectedColorTheme.Id, Is.EqualTo("Light"),
                "the dropdown selection reverts to the based-on theme");
        });
    }

    [Test]
    public void ResetColors_ClearsHasCustomizations()
    {
        var sut = new SettingsViewModel(() => { });
        sut.ColorSlots.First(s => s.Key == "Background").Color = Colors.Magenta;
        Assume.That(sut.HasCustomizations, Is.True);

        sut.ResetColorsCommand.Execute(null);

        Assert.That(sut.HasCustomizations, Is.False);
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
    public void SwitchProfile_InvokesInjectedCallback()
    {
        var called = false;
        var sut = new SettingsViewModel(() => { }, switchProfile: () => called = true);

        sut.SwitchProfileCommand.Execute(null);

        Assert.That(called, Is.True);
    }
}
