using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;

namespace Collectary.Presentation.Localization;

public enum AppTheme { Light, Dark }

public record ColorThemeInfo(string Id, string DisplayName, bool IsDark);

public record SkinInfo(string Id, string DisplayName);

public class ThemeService
{
    public static readonly ThemeService Instance = new();

    private const string AssemblyRoot = "avares://Collectary.UI";

    private ResourceInclude? _palette;
    private ResourceDictionary? _accentOverride;
    private ResourceDictionary? _customOverride;
    private ResourceDictionary? _systemAccentOverride;
    private IStyle? _skin;

    private ThemeService() { }

    public IReadOnlyList<ColorThemeInfo> Themes { get; } =
    [
        new("Light", "Light", false),
        new("Dark", "Dark", true),
        new("Nord", "Nord", true),
        new("Dracula", "Dracula", true),
        new("SolarizedLight", "Solarized Light", false),
        new("SolarizedDark", "Solarized Dark", true),
        new("CatppuccinLatte", "Catppuccin Latte", false),
        new("CatppuccinMocha", "Catppuccin Mocha", true),
        new("GruvboxLight", "Gruvbox Light", false),
        new("GruvboxDark", "Gruvbox Dark", true),
        new("HighContrast", "High Contrast", true),
        new("OneDark", "One Dark", true),
        new("Graphite", "Graphite", true),
    ];

    public IReadOnlyList<SkinInfo> Skins { get; } =
    [
        new("Windows11", "Windows 11"),
        new("Flat", "Flat"),
        new("Classic", "Classic"),
    ];

    private IReadOnlyList<string> SystemAccentKeys { get; } =
    [
        "SystemAccentColor",
        "SystemAccentColorLight1", "SystemAccentColorLight2", "SystemAccentColorLight3",
        "SystemAccentColorDark1", "SystemAccentColorDark2", "SystemAccentColorDark3",
    ];

    public string CurrentColorThemeId { get; private set; } = "Light";
    public string CurrentSkinId { get; private set; } = "Windows11";
    public Color? CurrentAccent { get; private set; }
    public IReadOnlyDictionary<string, Color> CurrentCustomColors { get; private set; } =
        new Dictionary<string, Color>();

    public AppTheme Current => CurrentColorThemeId == "Dark" ? AppTheme.Dark : AppTheme.Light;

    public void Apply(AppTheme theme) =>
        ApplyColorTheme(theme == AppTheme.Dark ? "Dark" : "Light");

    public void ApplyColorTheme(string id)
    {
        var info = Themes.FirstOrDefault(t => t.Id == id) ?? Themes[0];
        CurrentColorThemeId = info.Id;

        var app = Application.Current;
        if (app is null) return;

        var uri = new Uri($"{AssemblyRoot}/Themes/Colors.{info.Id}.axaml");
        var dict = new ResourceInclude(uri) { Source = uri };
        var merged = app.Resources.MergedDictionaries;

        var existing = _palette is not null && merged.Contains(_palette)
            ? _palette
            : FindExistingPalette(merged);
        if (existing is not null)
            merged[merged.IndexOf(existing)] = dict;
        else
            merged.Insert(0, dict);
        _palette = dict;

        app.RequestedThemeVariant = info.IsDark ? ThemeVariant.Dark : ThemeVariant.Light;

        ReapplyOverrides();
    }

    private ResourceInclude? FindExistingPalette(IList<Avalonia.Controls.IResourceProvider> merged)
    {
        var palettePrefix = $"{AssemblyRoot}/Themes/Colors.";
        return merged.OfType<ResourceInclude>()
            .FirstOrDefault(r => r.Source is { } s && s.OriginalString.StartsWith(palettePrefix, StringComparison.Ordinal));
    }

    public void ApplySkin(string id)
    {
        var info = Skins.FirstOrDefault(s => s.Id == id) ?? Skins[0];
        CurrentSkinId = info.Id;

        var app = Application.Current;
        if (app is null) return;

        var uri = new Uri($"{AssemblyRoot}/Themes/Skins/Skin.{info.Id}.axaml");
        var style = new StyleInclude((Uri?)null) { Source = uri };

        if (_skin is not null && app.Styles.Contains(_skin))
            app.Styles[app.Styles.IndexOf(_skin)] = style;
        else
            app.Styles.Add(style);
        _skin = style;
    }

    public void ApplyAccent(Color? accent)
    {
        CurrentAccent = accent;
        ReapplyOverrides();
    }

    public void ApplyCustomColors(IReadOnlyDictionary<string, Color>? overrides)
    {
        CurrentCustomColors = overrides is null
            ? new Dictionary<string, Color>()
            : new Dictionary<string, Color>(overrides);
        ReapplyOverrides();
    }

    public void ApplyCustomColors(IReadOnlyDictionary<string, string>? hex)
    {
        var parsed = new Dictionary<string, Color>();
        if (hex is not null)
        {
            foreach (var (key, value) in hex)
            {
                if (Color.TryParse(value, out var color))
                    parsed[key] = color;
            }
        }

        ApplyCustomColors(parsed);
    }

    private void ReapplyOverrides()
    {
        var app = Application.Current;
        if (app is null) return;

        var merged = app.Resources.MergedDictionaries;
        if (_accentOverride is not null && merged.Contains(_accentOverride))
            merged.Remove(_accentOverride);
        if (_customOverride is not null && merged.Contains(_customOverride))
            merged.Remove(_customOverride);
        if (_systemAccentOverride is not null && merged.Contains(_systemAccentOverride))
            merged.Remove(_systemAccentOverride);
        _accentOverride = null;
        _customOverride = null;
        _systemAccentOverride = null;

        if (CurrentAccent is { } accent)
        {
            _accentOverride = BuildAccentDictionary(accent);
            merged.Add(_accentOverride);
        }

        if (CurrentCustomColors.Count > 0)
        {
            _customOverride = BuildCustomDictionary(CurrentCustomColors);
            merged.Add(_customOverride);
        }

        if (ResolveEffectivePrimary(app) is { } primary)
        {
            _systemAccentOverride = BuildSystemAccentDictionary(primary);
            merged.Add(_systemAccentOverride);
        }
    }

    private Color? ResolveEffectivePrimary(Application app) =>
        app.TryGetResource("PrimaryColor", app.ActualThemeVariant, out var value) && value is Color primary
            ? primary
            : null;

    private ResourceDictionary BuildSystemAccentDictionary(Color primary)
    {
        var dict = new ResourceDictionary();
        foreach (var key in SystemAccentKeys)
            dict[key] = primary;
        return dict;
    }

    private ResourceDictionary BuildCustomDictionary(IReadOnlyDictionary<string, Color> overrides)
    {
        var dict = new ResourceDictionary();
        foreach (var (key, color) in overrides)
            Set(dict, key, color);
        return dict;
    }

    private ResourceDictionary BuildAccentDictionary(Color accent)
    {
        var dark = Luminance(accent) < 0.5;
        var hover = AdjustLightness(accent, dark ? 0.08 : -0.06);
        var pressed = AdjustLightness(accent, dark ? 0.16 : -0.12);
        var foreground = ContrastForeground(accent);

        var dict = new ResourceDictionary();
        Set(dict, "Primary", accent);
        Set(dict, "PrimaryHover", hover);
        Set(dict, "PrimaryPressed", pressed);
        Set(dict, "PrimaryForeground", foreground);
        Set(dict, "FocusRing", accent);
        Set(dict, "BorderStrong", accent);
        return dict;
    }

    private void Set(ResourceDictionary dict, string name, Color color)
    {
        dict[$"{name}Color"] = color;
        dict[$"{name}Brush"] = new SolidColorBrush(color);
    }

    internal double Luminance(Color c)
    {
        double Channel(double v)
        {
            v /= 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    internal Color ContrastForeground(Color c) =>
        Luminance(c) < 0.45 ? Colors.White : Color.FromRgb(0x21, 0x21, 0x21);

    internal Color AdjustLightness(Color c, double delta)
    {
        var (h, s, l) = ToHsl(c);
        l = Math.Clamp(l + delta, 0.0, 1.0);
        return FromHsl(h, s, l, c.A);
    }

    private (double H, double S, double L) ToHsl(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
        double h = 0, s, l = (max + min) / 2.0;
        double d = max - min;

        if (d == 0)
        {
            s = 0;
        }
        else
        {
            s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
            if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h /= 6.0;
        }

        return (h, s, l);
    }

    private Color FromHsl(double h, double s, double l, byte a)
    {
        double r, g, b;
        if (s == 0)
        {
            r = g = b = l;
        }
        else
        {
            double Hue(double p, double q, double t)
            {
                if (t < 0) t += 1;
                if (t > 1) t -= 1;
                if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
                if (t < 1.0 / 2.0) return q;
                if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
                return p;
            }

            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;
            r = Hue(p, q, h + 1.0 / 3.0);
            g = Hue(p, q, h);
            b = Hue(p, q, h - 1.0 / 3.0);
        }

        return Color.FromArgb(a, (byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255));
    }
}
