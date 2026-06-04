using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public record LanguageOption(string Code, string DisplayName);

public partial class SettingsViewModel : ViewModelBase
{
    private readonly Action _navigateToSystemFields;
    private readonly Func<Task<string?>>? _pickFolder;
    private readonly Action? _onSyncChanged;
    private bool _loadingSync;

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

    [ObservableProperty]
    public partial string? SyncLocation { get; set; }

    [ObservableProperty]
    public partial bool AutoSyncEnabled { get; set; }

    [ObservableProperty]
    public partial int AutoSyncIntervalMinutes { get; set; }

    public bool IsSyncConfigured => !string.IsNullOrWhiteSpace(SyncLocation);

    partial void OnSyncLocationChanged(string? value)
    {
        OnPropertyChanged(nameof(IsSyncConfigured));
        SaveSyncPreferences();
    }

    partial void OnAutoSyncEnabledChanged(bool value) => SaveSyncPreferences();

    partial void OnAutoSyncIntervalMinutesChanged(int value) => SaveSyncPreferences();

    [RelayCommand]
    private async Task ChooseSyncFolder()
    {
        if (_pickFolder is null) return;
        var folder = await _pickFolder();
        if (!string.IsNullOrWhiteSpace(folder)) SyncLocation = folder;
    }

    [RelayCommand]
    private void DisableSync() => SyncLocation = null;

    private void SaveSyncPreferences()
    {
        if (_loadingSync) return;
        AppPreferences.Update(p => p with
        {
            SyncLocation = string.IsNullOrWhiteSpace(SyncLocation) ? null : SyncLocation,
            AutoSyncEnabled = AutoSyncEnabled,
            AutoSyncIntervalMinutes = AutoSyncIntervalMinutes < 1 ? 1 : AutoSyncIntervalMinutes,
        });
        _onSyncChanged?.Invoke();
    }

    public SettingsViewModel(Action navigateToSystemFields, Func<Task<string?>>? pickFolder = null, Action? onSyncChanged = null)
    {
        _navigateToSystemFields = navigateToSystemFields;
        _pickFolder = pickFolder;
        _onSyncChanged = onSyncChanged;
        var currentCode = LocalizationService.Instance.CurrentCode;
        SelectedLanguage = LanguageOptions.FirstOrDefault(o => o.Code == currentCode) ?? LanguageOptions[0];
        CurrentTheme = ThemeService.Instance.Current;

        _loadingSync = true;
        var prefs = AppPreferences.Load();
        SyncLocation = prefs.SyncLocation;
        AutoSyncEnabled = prefs.AutoSyncEnabled;
        AutoSyncIntervalMinutes = prefs.AutoSyncIntervalMinutes;
        _loadingSync = false;
    }

    private void SavePreferences() =>
        AppPreferences.Update(p => p with
        {
            Theme = CurrentTheme,
            Language = SelectedLanguage?.Code ?? "en"
        });
}
