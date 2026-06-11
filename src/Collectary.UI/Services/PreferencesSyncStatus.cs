using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.UI.Services;

public class PreferencesSyncStatus : ISyncStatus
{
    public bool IsConfigured
    {
        get
        {
            var prefs = AppPreferences.Load();
            return prefs.SyncProvider switch
            {
                CloudProvider.OneDrive => !string.IsNullOrWhiteSpace(prefs.OneDriveRootFolderId),
                CloudProvider.GoogleDrive => !string.IsNullOrWhiteSpace(prefs.GoogleDriveRootFolderId),
                _ => !string.IsNullOrWhiteSpace(prefs.SyncLocation),
            };
        }
    }

    public string LocationLabel
    {
        get
        {
            var prefs = AppPreferences.Load();
            return prefs.SyncProvider switch
            {
                CloudProvider.OneDrive => Describe("Settings_Provider_OneDrive", prefs.OneDriveRootFolderName),
                CloudProvider.GoogleDrive => Describe("Settings_Provider_GoogleDrive", prefs.GoogleDriveRootFolderName),
                _ => string.IsNullOrWhiteSpace(prefs.SyncLocation)
                    ? LocalizationService.Instance["Settings_Provider_Folder"]
                    : prefs.SyncLocation,
            };
        }
    }

    private string Describe(string providerKey, string? folderName)
    {
        var provider = LocalizationService.Instance[providerKey];
        return string.IsNullOrWhiteSpace(folderName) ? provider : $"{provider} ({folderName})";
    }
}
