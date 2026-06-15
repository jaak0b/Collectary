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
    public void ApplyColorTheme_WhenPaletteUntracked_InsertsWithoutClobberingOtherDictionaries()
    {
        var app = Application.Current!;
        var merged = app.Resources.MergedDictionaries;
        var snapshot = merged.ToList();
        try
        {
            merged.Clear();
            var sentinel = new Avalonia.Controls.ResourceDictionary { ["SentinelColor"] = Colors.HotPink };
            merged.Add(sentinel);

            ThemeService.Instance.ApplyColorTheme("Dark");

            Assert.Multiple(() =>
            {
                Assert.That(merged, Does.Contain(sentinel),
                    "applying a theme must not overwrite an unrelated (non-palette) merged dictionary");
                Assert.That(Resource<Color>("SentinelColor"), Is.EqualTo(Colors.HotPink));
                Assert.That(Resource<Color>("BackgroundColor"), Is.EqualTo(Color.Parse("#121212")));
            });
        }
        finally
        {
            merged.Clear();
            foreach (var d in snapshot) merged.Add(d);
            ThemeService.Instance.ApplyColorTheme("Light");
        }
    }

    [Test]
    public void ApplyColorTheme_WhenPaletteUntracked_ReplacesExistingPaletteBySource_WithoutDuplicating()
    {
        var app = Application.Current!;
        var merged = app.Resources.MergedDictionaries;
        var snapshot = merged.ToList();
        try
        {
            merged.Clear();
            var decoyUri = new Uri("avares://Collectary.UI/Controls/FieldEditorScaffold.axaml");
            var decoy = new Avalonia.Markup.Xaml.Styling.ResourceInclude(decoyUri) { Source = decoyUri };
            merged.Add(decoy);
            var lightUri = new Uri("avares://Collectary.UI/Themes/Colors.Light.axaml");
            merged.Add(new Avalonia.Markup.Xaml.Styling.ResourceInclude(lightUri) { Source = lightUri });

            ThemeService.Instance.ApplyColorTheme("Dark");

            Assert.Multiple(() =>
            {
                Assert.That(merged, Does.Contain(decoy),
                    "a non-palette UI resource dictionary must never be mistaken for the palette and replaced");
                Assert.That(Resource<Color>("BackgroundColor"), Is.EqualTo(Color.Parse("#121212")),
                    "the Colors.* palette is the dictionary located by source and replaced in place");
            });
        }
        finally
        {
            merged.Clear();
            foreach (var d in snapshot) merged.Add(d);
            ThemeService.Instance.ApplyColorTheme("Light");
        }
    }

    [Test]
    public void ApplyColorTheme_WhenExistingPaletteIsSourcelessDictionary_ReplacesItSoNewPaletteWins()
    {
        var app = Application.Current!;
        var merged = app.Resources.MergedDictionaries;
        var snapshot = merged.ToList();
        try
        {
            merged.Clear();
            var inlinedLight = new Avalonia.Controls.ResourceDictionary
            {
                ["BackgroundColor"] = Color.Parse("#FFFFFF"),
                ["SurfaceColor"] = Color.Parse("#FAFAFA"),
            };
            merged.Add(inlinedLight);

            ThemeService.Instance.ApplyColorTheme("Dark");

            Assert.That(Resource<Color>("BackgroundColor"), Is.EqualTo(Color.Parse("#121212")),
                "a source-less inlined palette (how compiled XAML <ResourceInclude> entries materialise) must be replaced in place, not shadowed by a duplicate inserted at index 0");
        }
        finally
        {
            merged.Clear();
            foreach (var d in snapshot) merged.Add(d);
            ThemeService.Instance.ApplyColorTheme("Light");
        }
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
            Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(Avalonia.Styling.ThemeVariant.Light),
                "a light theme must request the Light variant");
        });
    }

    [Test]
    public void ApplyColorTheme_RetintsSystemAccentRampToThemePrimary()
    {
        ThemeService.Instance.ApplyColorTheme("Graphite");
        var primary = Resource<Color>("PrimaryColor");

        Assert.Multiple(() =>
        {
            Assert.That(Resource<Color>("SystemAccentColor"), Is.EqualTo(primary),
                "stock accent controls (checkbox, slider) must follow the theme accent, not the OS accent");
            Assert.That(Resource<Color>("SystemAccentColorLight1"), Is.EqualTo(primary));
            Assert.That(Resource<Color>("SystemAccentColorLight2"), Is.EqualTo(primary));
            Assert.That(Resource<Color>("SystemAccentColorLight3"), Is.EqualTo(primary));
            Assert.That(Resource<Color>("SystemAccentColorDark1"), Is.EqualTo(primary));
            Assert.That(Resource<Color>("SystemAccentColorDark2"), Is.EqualTo(primary));
            Assert.That(Resource<Color>("SystemAccentColorDark3"), Is.EqualTo(primary));
        });
    }

    [Test]
    public void ApplyAccent_RetintsSystemAccentRamp()
    {
        ThemeService.Instance.ApplyColorTheme("Light");
        ThemeService.Instance.ApplyAccent(Colors.Red);

        Assert.That(Resource<Color>("SystemAccentColor"), Is.EqualTo(Colors.Red));
    }

    [Test]
    public void ApplyCustomColors_PrimaryOverride_RetintsSystemAccentRamp()
    {
        ThemeService.Instance.ApplyColorTheme("Light");
        ThemeService.Instance.ApplyCustomColors(new Dictionary<string, Color> { ["Primary"] = Colors.Green });

        Assert.That(Resource<Color>("SystemAccentColor"), Is.EqualTo(Colors.Green));
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
    public void EveryColorTheme_DefinesWarningColorAndBrush()
    {
        foreach (var id in ThemeService.Instance.Themes.Select(t => t.Id))
        {
            ThemeService.Instance.ApplyColorTheme(id);

            Assert.Multiple(() =>
            {
                Assert.That(Resource<SolidColorBrush>("WarningBrush"), Is.Not.Null,
                    $"theme '{id}' must define WarningBrush so the warning text always has a colour");
                Assert.That(Resource<Color?>("WarningColor"), Is.Not.Null,
                    $"theme '{id}' must define WarningColor");
            });
        }
    }

    [Test]
    public void ApplyCustomColors_WarningOverride_SetsColorAndBrush()
    {
        ThemeService.Instance.ApplyColorTheme("Light");
        ThemeService.Instance.ApplyCustomColors(new Dictionary<string, Color>
        {
            ["Warning"] = Colors.Magenta,
        });

        Assert.Multiple(() =>
        {
            Assert.That(Resource<Color>("WarningColor"), Is.EqualTo(Colors.Magenta));
            Assert.That(Resource<SolidColorBrush>("WarningBrush").Color, Is.EqualTo(Colors.Magenta));
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
