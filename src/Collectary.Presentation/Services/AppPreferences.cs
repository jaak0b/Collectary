using System.Text.Json;
using Collectary.UI.Localization;

namespace Collectary.UI.Services;

public record AppPreferencesData(
    AppTheme Theme = AppTheme.Light,
    string Language = "en",
    double FieldPaneRatio = 0.4,
    bool SidebarOpen = true,
    double SidebarWidth = 260);

public static class AppPreferences
{
    internal static string FilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Collectary", "preferences.json");

    public static AppPreferencesData Load()
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

    public static void Save(AppPreferencesData data)
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
