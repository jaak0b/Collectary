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

    [ObservableProperty]
    public partial SyncNoticeSeverity Severity { get; set; }

    [ObservableProperty]
    public partial string? LastResultText { get; set; }

    public event Action? Synced;

    public Action? CloseRequested { get; set; }

    public bool IsConfigured => _status.IsConfigured;

    public bool NeedsAttention => Severity != SyncNoticeSeverity.None;

    public bool IsError => Severity == SyncNoticeSeverity.Error;

    public string LastSyncText => LastSyncedAt is null
        ? LocalizationService.Instance["Sync_Never"]
        : string.Format(LocalizationService.Instance["Sync_LastAt"], LastSyncedAt.Value.ToLocalTime());

    public SyncViewModel(ISyncService sync, ISyncStatus status, IUiDispatcher ui, IBackgroundRunner background)
    {
        _sync = sync;
        _status = status;
        _ui = ui;
        _background = background;
    }

    partial void OnLastSyncedAtChanged(DateTime? value) => OnPropertyChanged(nameof(LastSyncText));

    partial void OnSeverityChanged(SyncNoticeSeverity value)
    {
        OnPropertyChanged(nameof(NeedsAttention));
        OnPropertyChanged(nameof(IsError));
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
            Severity = SyncNoticeSeverity.None;
            LastResultText = null;
        });
        try
        {
            var result = await _background.RunAsync(() => _sync.SyncAsync());
            _ui.Post(() =>
            {
                if (result.BackendUnavailable)
                {
                    ErrorMessage = string.Format(LocalizationService.Instance["Sync_Unavailable"], _status.LocationLabel);
                    Severity = SyncNoticeSeverity.Advisory;
                    return;
                }

                LastSyncedAt = DateTime.UtcNow;
                ErrorMessage = BuildPartialNotice(result);
                Severity = ErrorMessage is null ? SyncNoticeSeverity.None : SyncNoticeSeverity.Advisory;
                LastResultText = string.Format(
                    LocalizationService.Instance["Sync_Result"], result.Pushed, result.Pulled);
                Synced?.Invoke();
            });
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Sync failed");
            _ui.Post(() =>
            {
                ErrorMessage = LocalizationService.Instance["Sync_Error"];
                Severity = SyncNoticeSeverity.Error;
            });
        }
        finally
        {
            await OnUiAsync(() => IsSyncing = false);
        }
    }

    private Task OnUiAsync(Action action)
    {
        var completion = new TaskCompletionSource();
        _ui.Post(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        return completion.Task;
    }

    public void ReportError() => _ui.Post(() =>
    {
        ErrorMessage = LocalizationService.Instance["Sync_Error"];
        Severity = SyncNoticeSeverity.Error;
    });

    private string? BuildPartialNotice(SyncResult result)
    {
        var clauses = new List<string>();
        if (result.Skipped > 0)
            clauses.Add(string.Format(LocalizationService.Instance["Sync_PartialItems"], result.Skipped));
        if (result.UnreadableDevices > 0)
            clauses.Add(string.Format(LocalizationService.Instance["Sync_PartialDevices"], result.UnreadableDevices));
        if (result.ImagesFailed > 0)
            clauses.Add(string.Format(LocalizationService.Instance["Sync_PartialImages"], result.ImagesFailed));

        return clauses.Count == 0
            ? null
            : string.Format(
                LocalizationService.Instance["Sync_Partial"],
                string.Join(LocalizationService.Instance["Sync_PartialSeparator"], clauses));
    }
}
