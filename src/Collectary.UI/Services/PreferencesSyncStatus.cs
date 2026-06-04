using Collectary.Core.Domain;
using Collectary.Core.Ports;
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

    public int TombstoneRetentionDays => AppPreferences.Load().TombstoneRetentionDays;
}
