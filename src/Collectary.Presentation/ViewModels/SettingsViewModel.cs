using System.Collections.ObjectModel;
using Avalonia.Media;
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
    private readonly Func<Task<bool>>? _exportBackup;
    private readonly Func<Task<BackupImportResult?>>? _importBackup;
    private readonly Action? _logout;
    private bool _loadingSync;
    private bool _loadingAppearance;

    public bool IsWeb => OperatingSystem.IsBrowser();

    [ObservableProperty]
    public partial bool CanLogout { get; set; }

    [ObservableProperty]
    public partial bool RequireLoginOnWeb { get; set; }

    partial void OnRequireLoginOnWebChanged(bool value)
    {
        if (_loadingSync) return;
        AppPreferences.Update(p => p with { RequireLoginOnWeb = value });
    }

    [RelayCommand]
    private void Logout() => _logout?.Invoke();

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

    public IReadOnlyList<SkinInfo> Skins => ThemeService.Instance.Skins;
    public IReadOnlyList<ColorThemeInfo> ColorThemes => ThemeService.Instance.Themes;

    [ObservableProperty]
    public partial SkinInfo SelectedSkin { get; set; }

    [ObservableProperty]
    public partial ColorThemeInfo SelectedColorTheme { get; set; }

    [ObservableProperty]
    public partial Color AccentColor { get; set; }

    [ObservableProperty]
    public partial bool HasCustomAccent { get; set; }

    [ObservableProperty]
    public partial bool ExpertColorMode { get; set; }

    public IReadOnlyList<FieldLabelLayoutOption> FieldLabelLayoutOptions { get; } =
    [
        new(FieldLabelLayout.Beside, LocalizationService.Instance["FieldLabel_Beside"]),
        new(FieldLabelLayout.Above, LocalizationService.Instance["FieldLabel_Above"]),
        new(FieldLabelLayout.Adaptive, LocalizationService.Instance["FieldLabel_Adaptive"]),
    ];

    [ObservableProperty]
    public partial FieldLabelLayoutOption SelectedFieldLabelLayout { get; set; }

    partial void OnSelectedFieldLabelLayoutChanged(FieldLabelLayoutOption? value)
    {
        if (_loadingAppearance || value?.Value is not { } layout) return;
        AppPreferences.Update(p => p with { FieldLabelLayout = layout });
    }

    public ObservableCollection<CustomColorSlot> ColorSlots { get; } = [];

    private readonly IReadOnlyList<(string Key, string LabelKey, bool IsEasy)> _slotDefinitions =
    [
        ("Background", "Color_Background", true),
        ("Surface", "Color_Surface", true),
        ("SurfaceAlt", "Color_SurfaceAlt", false),
        ("Primary", "Color_Primary", false),
        ("PrimaryHover", "Color_PrimaryHover", false),
        ("PrimaryPressed", "Color_PrimaryPressed", false),
        ("PrimaryForeground", "Color_PrimaryForeground", false),
        ("TextPrimary", "Color_TextPrimary", true),
        ("TextSecondary", "Color_TextSecondary", false),
        ("TextDisabled", "Color_TextDisabled", false),
        ("Border", "Color_Border", false),
        ("BorderStrong", "Color_BorderStrong", false),
        ("ControlHover", "Color_ControlHover", false),
        ("FocusRing", "Color_FocusRing", false),
        ("SidebarBackground", "Color_SidebarBackground", true),
        ("SidebarSelected", "Color_SidebarSelected", false),
        ("Danger", "Color_Danger", false),
        ("DangerForeground", "Color_DangerForeground", false),
    ];

    partial void OnExpertColorModeChanged(bool value)
    {
        UpdateSlotVisibility();
        if (_loadingAppearance) return;
        SaveAppearance();
    }

    private void UpdateSlotVisibility()
    {
        foreach (var slot in ColorSlots)
            slot.IsRowVisible = slot.IsEasy || ExpertColorMode;
    }

    private void OnColorSlotChanged(CustomColorSlot slot)
    {
        if (_loadingAppearance) return;
        ThemeService.Instance.ApplyCustomColors(CurrentOverrideMap());
        SaveAppearance();
    }

    private Dictionary<string, Color> CurrentOverrideMap() =>
        ColorSlots.Where(s => s.IsOverridden).ToDictionary(s => s.Key, s => s.Color);

    private Color CurrentColorFor(string key)
    {
        if (ThemeService.Instance.CurrentCustomColors.TryGetValue(key, out var custom))
            return custom;
        if (Avalonia.Application.Current?.TryGetResource(
                $"{key}Color", Avalonia.Application.Current.ActualThemeVariant, out var value) == true
            && value is Color color)
            return color;
        return Colors.Gray;
    }

    [RelayCommand]
    private void ResetColors()
    {
        _loadingAppearance = true;
        ThemeService.Instance.ApplyCustomColors((IReadOnlyDictionary<string, Color>?)null);
        ThemeService.Instance.ApplyAccent(null);
        HasCustomAccent = false;
        foreach (var slot in ColorSlots)
            slot.Revert(CurrentColorFor(slot.Key));
        AccentColor = CurrentThemePrimary();
        _loadingAppearance = false;
        SaveAppearance();
    }

    partial void OnSelectedSkinChanged(SkinInfo? value)
    {
        if (_loadingAppearance || value is null) return;
        ThemeService.Instance.ApplySkin(value.Id);
        SaveAppearance();
    }

    partial void OnSelectedColorThemeChanged(ColorThemeInfo? value)
    {
        if (_loadingAppearance || value is null) return;
        ThemeService.Instance.ApplyColorTheme(value.Id);
        foreach (var slot in ColorSlots.Where(s => !s.IsOverridden))
            slot.Revert(CurrentColorFor(slot.Key));
        SaveAppearance();
    }

    partial void OnAccentColorChanged(Color value)
    {
        if (_loadingAppearance) return;
        HasCustomAccent = true;
        ThemeService.Instance.ApplyAccent(value);
        SaveAppearance();
    }

    [RelayCommand]
    private void ResetAccent()
    {
        HasCustomAccent = false;
        ThemeService.Instance.ApplyAccent(null);
        _loadingAppearance = true;
        AccentColor = CurrentThemePrimary();
        _loadingAppearance = false;
        SaveAppearance();
    }

    private void SaveAppearance()
    {
        if (_loadingAppearance) return;
        var custom = ColorSlots.Where(s => s.IsOverridden)
            .ToDictionary(s => s.Key, s => s.Color.ToString());
        AppPreferences.Update(p => p with
        {
            Skin = SelectedSkin?.Id ?? "Windows11",
            ColorTheme = SelectedColorTheme?.Id ?? "Light",
            AccentColor = HasCustomAccent ? AccentColor.ToString() : null,
            CustomColors = custom.Count > 0 ? custom : null,
            ExpertColorMode = ExpertColorMode,
        });
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

    [ObservableProperty]
    public partial string? BackupStatus { get; set; }

    [RelayCommand]
    private async Task ExportBackup()
    {
        if (_exportBackup is null) return;
        BackupStatus = null;
        try
        {
            if (await _exportBackup())
                BackupStatus = LocalizationService.Instance["Backup_Exported"];
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Backup export failed");
            BackupStatus = LocalizationService.Instance["Backup_Error"];
        }
    }

    [RelayCommand]
    private async Task ImportBackup()
    {
        if (_importBackup is null) return;
        BackupStatus = null;
        try
        {
            var result = await _importBackup();
            if (result is null) return;
            BackupStatus = result.HasConflicts
                ? string.Format(
                    LocalizationService.Instance["Backup_ImportedWithConflicts"],
                    result.Applied,
                    string.Join(", ", result.Conflicts.Select(c => c.LocalLabel)))
                : string.Format(LocalizationService.Instance["Backup_Imported"], result.Applied);
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Backup import failed");
            BackupStatus = LocalizationService.Instance["Backup_Error"];
        }
    }

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
        Func<string?>? detectInstalledCloudFolder = null,
        Func<Task<bool>>? exportBackup = null,
        Func<Task<BackupImportResult?>>? importBackup = null,
        Action? logout = null,
        bool canLogout = false)
    {
        _navigateToSystemFields = navigateToSystemFields;
        _pickFolder = pickFolder;
        _onSyncChanged = onSyncChanged;
        _connectCloud = connectCloud;
        _pickCloudFolder = pickCloudFolder;
        _disconnectCloud = disconnectCloud;
        _detectInstalledCloudFolder = detectInstalledCloudFolder;
        _exportBackup = exportBackup;
        _importBackup = importBackup;
        _logout = logout;
        CanLogout = canLogout;
        var currentCode = LocalizationService.Instance.CurrentCode;
        SelectedLanguage = LanguageOptions.FirstOrDefault(o => o.Code == currentCode) ?? LanguageOptions[0];

        _loadingSync = true;
        _loadingAppearance = true;
        var prefs = AppPreferences.Load();
        SyncProvider = prefs.SyncProvider;
        SyncLocation = prefs.SyncLocation;
        AutoSyncEnabled = prefs.AutoSyncEnabled;
        AutoSyncIntervalMinutes = prefs.AutoSyncIntervalMinutes;
        RequireLoginOnWeb = prefs.RequireLoginOnWeb ?? true;

        SelectedSkin = Skins.FirstOrDefault(s => s.Id == ThemeService.Instance.CurrentSkinId) ?? Skins[0];
        SelectedColorTheme = ColorThemes.FirstOrDefault(t => t.Id == ThemeService.Instance.CurrentColorThemeId) ?? ColorThemes[0];
        HasCustomAccent = ThemeService.Instance.CurrentAccent is not null;
        AccentColor = ThemeService.Instance.CurrentAccent ?? CurrentThemePrimary();
        ExpertColorMode = prefs.ExpertColorMode;
        SelectedFieldLabelLayout = FieldLabelLayoutOptions.First(o => o.Value == prefs.FieldLabelLayout);
        foreach (var (key, labelKey, isEasy) in _slotDefinitions)
        {
            var overridden = ThemeService.Instance.CurrentCustomColors.ContainsKey(key);
            ColorSlots.Add(new CustomColorSlot(key, labelKey, isEasy, CurrentColorFor(key), overridden, OnColorSlotChanged));
        }
        UpdateSlotVisibility();
        _loadingSync = false;
        _loadingAppearance = false;
    }

    private static Color CurrentThemePrimary()
    {
        if (Avalonia.Application.Current?.TryGetResource(
                "PrimaryColor", Avalonia.Application.Current.ActualThemeVariant, out var value) == true
            && value is Color color)
            return color;
        return Color.Parse("#2563EB");
    }

    private void SavePreferences() =>
        AppPreferences.Update(p => p with
        {
            Language = SelectedLanguage?.Code ?? "en"
        });
}
