namespace Collectary.Presentation.Services;

public static class DialogService
{
    public static IDialogService Instance { get; set; } = new NoopDialogService();

    private sealed class NoopDialogService : IDialogService
    {
        public Task<bool> ConfirmDeleteAsync(string itemName) => Task.FromResult(false);
        public Task ShowMessageAsync(string message, string title = "") => Task.CompletedTask;
        public Task ShowShareDialogAsync(ViewModels.ShareDialogViewModel viewModel) => Task.CompletedTask;
        public Task ShowSyncConflictsAsync(ViewModels.SyncViewModel viewModel) => Task.CompletedTask;
    }
}
