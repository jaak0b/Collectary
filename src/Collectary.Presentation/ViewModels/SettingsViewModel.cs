using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public record LanguageOption(string Code, string DisplayName);

public partial class SettingsViewModel : ViewModelBase
{
    private readonly Action _navigateToSystemFields;

    public IReadOnlyList<LanguageOption> LanguageOptions { get; } =
    [
        new("en", "English"),
        new("de", "Deutsch")
    ];

    [ObservableProperty]
    public partial LanguageOption SelectedLanguage { get; set; }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (value is null) return;
        LocalizationService.Instance.Apply(value.Code);
        SavePreferences();
    }

    [ObservableProperty]
    public partial AppTheme CurrentTheme { get; set; }

    public bool IsLightTheme => CurrentTheme == AppTheme.Light;
    public bool IsDarkTheme => CurrentTheme == AppTheme.Dark;

    partial void OnCurrentThemeChanged(AppTheme _)
    {
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));
    }

    [RelayCommand]
    private void SetTheme(string theme)
    {
        var next = theme == "Dark" ? AppTheme.Dark : AppTheme.Light;
        if (next == CurrentTheme) return;
        ThemeService.Instance.Apply(next);
        CurrentTheme = next;
        SavePreferences();
    }

    [RelayCommand]
    private void NavigateToSystemFields() => _navigateToSystemFields();

    public SettingsViewModel(Action navigateToSystemFields)
    {
        _navigateToSystemFields = navigateToSystemFields;
        var currentCode = LocalizationService.Instance.CurrentCode;
        SelectedLanguage = LanguageOptions.FirstOrDefault(o => o.Code == currentCode) ?? LanguageOptions[0];
        CurrentTheme = ThemeService.Instance.Current;
    }

    private void SavePreferences() =>
        AppPreferences.Save(AppPreferences.Load() with
        {
            Theme = CurrentTheme,
            Language = SelectedLanguage?.Code ?? "en"
        });
}
