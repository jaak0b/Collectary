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

    public event Action? Synced;

    public Action? CloseRequested { get; set; }

    public bool IsConfigured => _status.IsConfigured;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool NeedsAttention => HasError;

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
            await _background.RunAsync(() => _sync.SyncAsync()).ConfigureAwait(false);
            _ui.Post(() =>
            {
                LastSyncedAt = DateTime.UtcNow;
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
}
