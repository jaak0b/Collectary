using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class SyncViewModel : ViewModelBase
{
    private readonly ISyncService _sync;
    private readonly ISyncStatus _status;
    private readonly IUiDispatcher _ui;
    private readonly IBackgroundRunner _background;

    [ObservableProperty]
    public partial bool IsSyncing { get; set; }

    [ObservableProperty]
    public partial DateTime? LastSyncedAt { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public ObservableCollection<SyncConflictViewModel> Conflicts { get; } = new();

    public event Action? Synced;

    public Action? CloseRequested { get; set; }

    public bool IsConfigured => _status.IsConfigured;

    public bool HasConflicts => Conflicts.Count > 0;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool NeedsAttention => HasConflicts || HasError;

    public string LastSyncText => LastSyncedAt is null
        ? LocalizationService.Instance["Sync_Never"]
        : string.Format(LocalizationService.Instance["Sync_LastAt"], LastSyncedAt.Value.ToLocalTime());

    public SyncViewModel(ISyncService sync, ISyncStatus status, IUiDispatcher ui, IBackgroundRunner background)
    {
        _sync = sync;
        _status = status;
        _ui = ui;
        _background = background;
        Conflicts.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasConflicts));
            OnPropertyChanged(nameof(NeedsAttention));
        };
    }

    partial void OnLastSyncedAtChanged(DateTime? value) => OnPropertyChanged(nameof(LastSyncText));

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(NeedsAttention));
    }

    public void Refresh() => OnPropertyChanged(nameof(IsConfigured));

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    [RelayCommand]
    public async Task SyncNow()
    {
        if (IsSyncing || !_status.IsConfigured) return;

        _ui.Post(() =>
        {
            IsSyncing = true;
            ErrorMessage = null;
        });
        try
        {
            var result = await _background.RunAsync(() => _sync.SyncAsync()).ConfigureAwait(false);
            _ui.Post(() =>
            {
                RefreshConflicts(result.Conflicts);
                if (result.Conflicts.Count == 0) LastSyncedAt = DateTime.UtcNow;
                Synced?.Invoke();
            });
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Sync failed");
            _ui.Post(() => ErrorMessage = LocalizationService.Instance["Sync_Error"]);
        }
        finally
        {
            _ui.Post(() => IsSyncing = false);
        }
    }

    public void ReportError() => _ui.Post(() => ErrorMessage = LocalizationService.Instance["Sync_Error"]);

    private void RefreshConflicts(IReadOnlyList<SyncConflict> conflicts)
    {
        Conflicts.Clear();
        foreach (var conflict in conflicts)
            Conflicts.Add(new SyncConflictViewModel(conflict, ResolveAsync));
    }

    private async Task ResolveAsync(SyncConflict conflict, bool keepLocal)
    {
        await _sync.ResolveAsync(conflict, keepLocal);

        Conflicts.Remove(Conflicts.FirstOrDefault(c => c.Conflict == conflict)!);

        if (Conflicts.Count == 0)
            await SyncNow();
    }
}
