using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public record LanguageOption(string Code, string DisplayName);

public partial class SettingsViewModel : ViewModelBase
{
    private readonly Action _navigateToSystemFields;
    private readonly Func<Task<string?>>? _pickFolder;
    private readonly Action? _onSyncChanged;
    private readonly Func<CloudProvider, Task<string?>>? _connectCloud;
    private readonly Func<CloudProvider, Task<CloudFolder?>>? _pickCloudFolder;
    private readonly Func<CloudProvider, Task>? _disconnectCloud;
    private readonly Func<string?>? _detectInstalledCloudFolder;
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

    public bool IsSyncConfigured => SyncProvider switch
    {
        CloudProvider.OneDrive or CloudProvider.GoogleDrive => IsCloudFolderChosen,
        _ => !string.IsNullOrWhiteSpace(SyncLocation),
    };

    partial void OnSyncLocationChanged(string? value)
    {
        OnPropertyChanged(nameof(IsSyncConfigured));
        SaveSyncPreferences();
    }

    public IReadOnlyList<CloudProvider> ProviderOptions { get; } =
        [CloudProvider.Folder, CloudProvider.OneDrive, CloudProvider.GoogleDrive];

    [ObservableProperty]
    public partial CloudProvider SyncProvider { get; set; }

    partial void OnSyncProviderChanged(CloudProvider value)
    {
        RaiseCloudProperties();
        if (_loadingSync) return;
        AppPreferences.Update(p => p with { SyncProvider = value });
        _onSyncChanged?.Invoke();
    }

    public bool IsCloudProvider => SyncProvider != CloudProvider.Folder;

    public bool IsCloudConnected => !string.IsNullOrWhiteSpace(CurrentAccount);

    public string? AccountLabel => CurrentAccount;

    public string ConnectionStatus => IsCloudConnected
        ? string.Format(LocalizationService.Instance["Cloud_Connected_As"], CurrentAccount)
        : LocalizationService.Instance["Cloud_NotConnected"];

    public bool IsCloudFolderChosen => !string.IsNullOrWhiteSpace(CurrentRootFolderId);

    public string? SelectedFolderName => SyncProvider switch
    {
        CloudProvider.OneDrive => AppPreferences.Load().OneDriveRootFolderName,
        CloudProvider.GoogleDrive => AppPreferences.Load().GoogleDriveRootFolderName,
        _ => null,
    };

    private string? CurrentAccount => SyncProvider switch
    {
        CloudProvider.OneDrive => AppPreferences.Load().OneDriveAccount,
        CloudProvider.GoogleDrive => AppPreferences.Load().GoogleDriveAccount,
        _ => null,
    };

    private string? CurrentRootFolderId => SyncProvider switch
    {
        CloudProvider.OneDrive => AppPreferences.Load().OneDriveRootFolderId,
        CloudProvider.GoogleDrive => AppPreferences.Load().GoogleDriveRootFolderId,
        _ => null,
    };

    [RelayCommand]
    private async Task Connect()
    {
        if (_connectCloud is null) return;
        var account = await _connectCloud(SyncProvider);
        if (string.IsNullOrWhiteSpace(account)) return;

        AppPreferences.Update(p => SyncProvider == CloudProvider.OneDrive
            ? p with { OneDriveAccount = account }
            : p with { GoogleDriveAccount = account });
        RaiseCloudProperties();
    }

    [RelayCommand]
    private async Task ChooseCloudFolder()
    {
        if (_pickCloudFolder is null) return;
        var folder = await _pickCloudFolder(SyncProvider);
        if (folder is null) return;

        AppPreferences.Update(p => SyncProvider == CloudProvider.OneDrive
            ? p with { OneDriveRootFolderId = folder.Id, OneDriveRootFolderName = folder.Name }
            : p with { GoogleDriveRootFolderId = folder.Id, GoogleDriveRootFolderName = folder.Name });
        RaiseCloudProperties();
        _onSyncChanged?.Invoke();
    }

    [RelayCommand]
    private async Task Disconnect()
    {
        if (_disconnectCloud is not null) await _disconnectCloud(SyncProvider);

        AppPreferences.Update(p => SyncProvider == CloudProvider.OneDrive
            ? p with { OneDriveAccount = null, OneDriveRootFolderId = null, OneDriveRootFolderName = null }
            : p with { GoogleDriveAccount = null, GoogleDriveRootFolderId = null, GoogleDriveRootFolderName = null });
        RaiseCloudProperties();
        _onSyncChanged?.Invoke();
    }

    [RelayCommand]
    private void AutoDetectLocalCloudFolder()
    {
        if (_detectInstalledCloudFolder is null) return;
        var path = _detectInstalledCloudFolder();
        if (string.IsNullOrWhiteSpace(path)) return;

        SyncProvider = CloudProvider.Folder;
        SyncLocation = path;
    }

    private void RaiseCloudProperties()
    {
        OnPropertyChanged(nameof(IsCloudProvider));
        OnPropertyChanged(nameof(IsCloudConnected));
        OnPropertyChanged(nameof(AccountLabel));
        OnPropertyChanged(nameof(ConnectionStatus));
        OnPropertyChanged(nameof(IsCloudFolderChosen));
        OnPropertyChanged(nameof(SelectedFolderName));
        OnPropertyChanged(nameof(IsSyncConfigured));
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

    public SettingsViewModel(
        Action navigateToSystemFields,
        Func<Task<string?>>? pickFolder = null,
        Action? onSyncChanged = null,
        Func<CloudProvider, Task<string?>>? connectCloud = null,
        Func<CloudProvider, Task<CloudFolder?>>? pickCloudFolder = null,
        Func<CloudProvider, Task>? disconnectCloud = null,
        Func<string?>? detectInstalledCloudFolder = null)
    {
        _navigateToSystemFields = navigateToSystemFields;
        _pickFolder = pickFolder;
        _onSyncChanged = onSyncChanged;
        _connectCloud = connectCloud;
        _pickCloudFolder = pickCloudFolder;
        _disconnectCloud = disconnectCloud;
        _detectInstalledCloudFolder = detectInstalledCloudFolder;
        var currentCode = LocalizationService.Instance.CurrentCode;
        SelectedLanguage = LanguageOptions.FirstOrDefault(o => o.Code == currentCode) ?? LanguageOptions[0];
        CurrentTheme = ThemeService.Instance.Current;

        _loadingSync = true;
        var prefs = AppPreferences.Load();
        SyncProvider = prefs.SyncProvider;
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
