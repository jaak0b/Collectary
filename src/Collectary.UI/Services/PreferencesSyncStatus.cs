using Collectary.Core.Ports;
using Collectary.Presentation.Services;

namespace Collectary.UI.Services;

public class PreferencesSyncStatus : ISyncStatus
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(AppPreferences.Load().SyncLocation);

    public int TombstoneRetentionDays => AppPreferences.Load().TombstoneRetentionDays;
}
