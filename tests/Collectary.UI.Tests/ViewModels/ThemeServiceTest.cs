using Avalonia;
using Avalonia.Media;
using Collectary.Presentation.Localization;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ThemeServiceTest
{
    [TearDown]
    public void TearDown()
    {
        ThemeService.Instance.ApplyCustomColors((IReadOnlyDictionary<string, Color>?)null);
        ThemeService.Instance.ApplyAccent(null);
        ThemeService.Instance.ApplyColorTheme("Light");
        ThemeService.Instance.ApplySkin("Windows11");
    }

    [Test]
    public void Themes_ContainAllExpectedIds_Unique()
    {
        var ids = ThemeService.Instance.Themes.Select(t => t.Id).ToList();

        Assert.That(ids, Is.Unique);
        Assert.That(ids, Is.SupersetOf(new[]
        {
            "Light", "Dark", "Nord", "Dracula", "SolarizedLight", "SolarizedDark",
            "CatppuccinLatte", "CatppuccinMocha", "GruvboxLight", "GruvboxDark",
            "HighContrast", "OneDark", "Graphite"
        }));
        Assert.That(ids, Has.Count.GreaterThanOrEqualTo(8));
    }

    [Test]
    public void Themes_IncludeGraphiteGreyDarkTheme()
    {
        var graphite = ThemeService.Instance.Themes.FirstOrDefault(t => t.Id == "Graphite");

        Assert.Multiple(() =>
        {
            Assert.That(graphite, Is.Not.Null);
            Assert.That(graphite!.IsDark, Is.True);
        });
    }

    [Test]
    public void ApplyColorTheme_Graphite_LoadsGreyPaletteWithGreySelection()
    {
        ThemeService.Instance.ApplyColorTheme("Graphite");

        Assert.Multiple(() =>
        {
            Assert.That(ThemeService.Instance.CurrentColorThemeId, Is.EqualTo("Graphite"));
            Assert.That(Resource<Color>("BackgroundColor"), Is.EqualTo(Color.Parse("#313338")));
            Assert.That(Resource<Color>("SurfaceColor"), Is.EqualTo(Color.Parse("#2B2D31")));
            Assert.That(Resource<Color>("SidebarSelectedColor"), Is.EqualTo(Color.Parse("#404249")),
                "Graphite highlights the selected item with a grey, not a coloured fill");
            Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(Avalonia.Styling.ThemeVariant.Dark));
        });
    }

    [Test]
    public void Skins_AreWindows11FlatClassic()
    {
        Assert.That(ThemeService.Instance.Skins.Select(s => s.Id),
            Is.EqualTo(new[] { "Windows11", "Flat", "Classic" }));
    }

    [Test]
    public void ContrastForeground_PicksReadableTextColor()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ThemeService.Instance.ContrastForeground(Colors.White), Is.EqualTo(Color.FromRgb(0x21, 0x21, 0x21)));
            Assert.That(ThemeService.Instance.ContrastForeground(Colors.Black), Is.EqualTo(Colors.White));
        });
    }

    [Test]
    public void Luminance_WhiteBrighterThanBlack()
    {
        var svc = ThemeService.Instance;
        Assert.That(svc.Luminance(Colors.White), Is.GreaterThan(svc.Luminance(Colors.Black)));
    }

    [Test]
    public void AdjustLightness_LightensAndDarkensDeterministically()
    {
        var svc = ThemeService.Instance;
        var mid = Color.FromRgb(0x80, 0x80, 0x80);

        var lighter = svc.AdjustLightness(mid, 0.2);
        var darker = svc.AdjustLightness(mid, -0.2);

        Assert.Multiple(() =>
        {
            Assert.That(svc.Luminance(lighter), Is.GreaterThan(svc.Luminance(mid)));
            Assert.That(svc.Luminance(darker), Is.LessThan(svc.Luminance(mid)));
            Assert.That(svc.AdjustLightness(mid, 0.2), Is.EqualTo(lighter));
        });
    }

    [Test]
    public void ApplyColorTheme_SwapsPaletteAndVariant()
    {
        ThemeService.Instance.ApplyColorTheme("Dark");
        var dark = Resource<Color>("BackgroundColor");
        var darkVariant = Application.Current!.RequestedThemeVariant;

        ThemeService.Instance.ApplyColorTheme("Light");
        var light = Resource<Color>("BackgroundColor");

        Assert.Multiple(() =>
        {
            Assert.That(ThemeService.Instance.CurrentColorThemeId, Is.EqualTo("Light"));
            Assert.That(dark, Is.Not.EqualTo(light));
            Assert.That(darkVariant, Is.EqualTo(Avalonia.Styling.ThemeVariant.Dark));
        });
    }

    [Test]
    public void ApplyAccent_OverridesPrimaryWithContrastForeground()
    {
        ThemeService.Instance.ApplyColorTheme("Light");
        ThemeService.Instance.ApplyAccent(Colors.Red);

        Assert.Multiple(() =>
        {
            Assert.That(Resource<Color>("PrimaryColor"), Is.EqualTo(Colors.Red));
            Assert.That(Resource<Color>("PrimaryForegroundColor"), Is.EqualTo(Colors.White));
            Assert.That(Resource<Color>("PrimaryHoverColor"), Is.Not.EqualTo(Colors.Red));
        });
    }

    [Test]
    public void ApplyAccent_Null_RevertsToThemePrimary()
    {
        ThemeService.Instance.ApplyColorTheme("Light");
        var themePrimary = Resource<Color>("PrimaryColor");

        ThemeService.Instance.ApplyAccent(Colors.Red);
        ThemeService.Instance.ApplyAccent(null);

        Assert.That(Resource<Color>("PrimaryColor"), Is.EqualTo(themePrimary));
    }

    [Test]
    public void ApplySkin_SwapsShapeTokens()
    {
        ThemeService.Instance.ApplySkin("Windows11");
        var win11 = Resource<CornerRadius>("ControlCornerRadius");

        ThemeService.Instance.ApplySkin("Flat");
        var flat = Resource<CornerRadius>("ControlCornerRadius");

        Assert.Multiple(() =>
        {
            Assert.That(ThemeService.Instance.CurrentSkinId, Is.EqualTo("Flat"));
            Assert.That(win11.TopLeft, Is.EqualTo(6));
            Assert.That(flat.TopLeft, Is.EqualTo(0));
        });
    }

    [Test]
    public void ApplyCustomColors_OverridesColorAndBrush()
    {
        ThemeService.Instance.ApplyColorTheme("Light");
        ThemeService.Instance.ApplyCustomColors(new Dictionary<string, Color>
        {
            ["Background"] = Colors.Magenta,
        });

        Assert.Multiple(() =>
        {
            Assert.That(Resource<Color>("BackgroundColor"), Is.EqualTo(Colors.Magenta));
            Assert.That(Resource<SolidColorBrush>("BackgroundBrush").Color, Is.EqualTo(Colors.Magenta));
            Assert.That(ThemeService.Instance.CurrentCustomColors, Does.ContainKey("Background"));
        });
    }

    [Test]
    public void ApplyCustomColors_Null_RevertsToPalette()
    {
        ThemeService.Instance.ApplyColorTheme("Light");
        var themeBackground = Resource<Color>("BackgroundColor");

        ThemeService.Instance.ApplyCustomColors(new Dictionary<string, Color> { ["Background"] = Colors.Magenta });
        ThemeService.Instance.ApplyCustomColors((IReadOnlyDictionary<string, Color>?)null);

        Assert.Multiple(() =>
        {
            Assert.That(Resource<Color>("BackgroundColor"), Is.EqualTo(themeBackground));
            Assert.That(ThemeService.Instance.CurrentCustomColors, Is.Empty);
        });
    }

    [Test]
    public void ApplyCustomColors_WinsOverAccent()
    {
        ThemeService.Instance.ApplyColorTheme("Light");
        ThemeService.Instance.ApplyAccent(Colors.Red);
        ThemeService.Instance.ApplyCustomColors(new Dictionary<string, Color> { ["Primary"] = Colors.Green });

        Assert.That(Resource<Color>("PrimaryColor"), Is.EqualTo(Colors.Green));
    }

    [Test]
    public void ApplyColorTheme_ReappliesCustomOverride()
    {
        ThemeService.Instance.ApplyCustomColors(new Dictionary<string, Color> { ["Background"] = Colors.Magenta });
        ThemeService.Instance.ApplyColorTheme("Dark");

        Assert.That(Resource<Color>("BackgroundColor"), Is.EqualTo(Colors.Magenta));
    }

    [Test]
    public void ApplyCustomColors_Hex_ParsesAndSkipsInvalid()
    {
        ThemeService.Instance.ApplyColorTheme("Light");
        ThemeService.Instance.ApplyCustomColors(new Dictionary<string, string>
        {
            ["Background"] = "#FF00FF",
            ["TextPrimary"] = "not-a-color",
        });

        Assert.Multiple(() =>
        {
            Assert.That(Resource<Color>("BackgroundColor"), Is.EqualTo(Colors.Magenta));
            Assert.That(ThemeService.Instance.CurrentCustomColors, Does.ContainKey("Background"));
            Assert.That(ThemeService.Instance.CurrentCustomColors, Does.Not.ContainKey("TextPrimary"));
        });
    }

    private static T Resource<T>(string key)
    {
        var app = Application.Current!;
        app.TryGetResource(key, app.ActualThemeVariant, out var value);
        return (T)value!;
    }
}
