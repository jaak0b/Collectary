using System.Text.Json;
using Collectary.Core.Domain;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.Services;

public record AppPreferencesData(
    AppTheme Theme = AppTheme.Light,
    string Language = "en",
    double FieldPaneRatio = 0.4,
    bool SidebarOpen = true,
    double SidebarWidth = 260,
    bool RequireLogin = true,
    string? SyncLocation = null,
    bool AutoSyncEnabled = false,
    int AutoSyncIntervalMinutes = 5,
    int TombstoneRetentionDays = 30,
    CloudProvider SyncProvider = CloudProvider.Folder,
    string? OneDriveRootFolderId = null,
    string? OneDriveRootFolderName = null,
    string? OneDriveAccount = null,
    string? GoogleDriveRootFolderId = null,
    string? GoogleDriveRootFolderName = null,
    string? GoogleDriveAccount = null,
    string ColorTheme = "Light",
    string Skin = "Windows11",
    string? AccentColor = null,
    Dictionary<string, string>? CustomColors = null,
    bool ExpertColorMode = false,
    FieldLabelLayout FieldLabelLayout = FieldLabelLayout.Adaptive)
{
    public string EffectiveColorTheme() =>
        ColorTheme == "Light" && Theme == AppTheme.Dark ? "Dark" : ColorTheme;
}

public static class AppPreferences
{
    private static readonly object Gate = new();

    internal static string FilePath { get; set; } = Path.Combine(AppDataPaths.Root, "preferences.json");

    public static AppPreferencesData Load()
    {
        lock (Gate) return LoadUnlocked();
    }

    public static void Save(AppPreferencesData data)
    {
        lock (Gate) SaveUnlocked(data);
    }

    public static AppPreferencesData Update(Func<AppPreferencesData, AppPreferencesData> mutate)
    {
        lock (Gate)
        {
            var updated = mutate(LoadUnlocked());
            SaveUnlocked(updated);
            return updated;
        }
    }

    private static AppPreferencesData LoadUnlocked()
    {
        try
        {
            if (!File.Exists(FilePath)) return new();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppPreferencesData>(json) ?? new();
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to load preferences; falling back to defaults");
            return new();
        }
    }

    private static void SaveUnlocked(AppPreferencesData data)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(data));
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to save preferences");
        }
    }
}
