using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Collectary.UI.Localization;

public enum AppTheme { Light, Dark }

public class ThemeService
{
    public static readonly ThemeService Instance = new();

    private AppTheme _current = AppTheme.Light;

    private ThemeService() { }

    public AppTheme Current => _current;

    public void Apply(AppTheme theme)
    {
        _current = theme;
        var uri = theme == AppTheme.Dark
            ? new Uri("avares://Collectary.UI/Themes/Colors.Dark.axaml")
            : new Uri("avares://Collectary.UI/Themes/Colors.Light.axaml");

        var dict = new ResourceInclude(uri) { Source = uri };
        Application.Current!.Resources.MergedDictionaries[0] = dict;
        Application.Current.RequestedThemeVariant = theme == AppTheme.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }
}
