using Collectary.Core.Ports;
using Collectary.Presentation.ViewModels;

namespace Collectary.Presentation.Services;

public sealed class NoopDialogService : IDialogService
{
    public Task<bool> ConfirmDeleteAsync(string itemName) => Task.FromResult(false);

    public Task<bool> ConfirmAsync(string message, string confirmLabel, string title) => Task.FromResult(false);

    public Task ShowMessageAsync(string message, string title = "") => Task.CompletedTask;

    public Task ShowSyncConflictsAsync(SyncViewModel viewModel) => Task.CompletedTask;

    public Task<CloudFolder?> ShowCloudFolderPickerAsync(CloudFolderPickerViewModel viewModel) =>
        Task.FromResult<CloudFolder?>(null);
}
