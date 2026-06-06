using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.ViewModels;

public partial class SyncConflictViewModel : ViewModelBase
{
    private readonly SyncConflict _conflict;
    private readonly Func<SyncConflict, bool, Task> _resolve;

    public SyncConflictViewModel(SyncConflict conflict, Func<SyncConflict, bool, Task> resolve)
    {
        _conflict = conflict;
        _resolve = resolve;
    }

    public string KindText => LocalizationService.Instance[_conflict.Kind switch
    {
        SyncEntityKind.Preset => "Sync_KindCollection",
        SyncEntityKind.Item => "Sync_KindItem",
        _ => "Sync_KindSharedField",
    }];

    public string LocalLabel => _conflict.LocalLabel;
    public string RemoteLabel => _conflict.RemoteLabel;

    [RelayCommand]
    private Task KeepMine() => _resolve(_conflict, true);

    [RelayCommand]
    private Task KeepTheirs() => _resolve(_conflict, false);
}
