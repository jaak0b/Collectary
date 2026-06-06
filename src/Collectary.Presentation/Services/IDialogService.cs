using Collectary.Core.Ports;
using Collectary.Presentation.ViewModels;

namespace Collectary.Presentation.Services;

public interface IDialogService
{
    Task<bool> ConfirmDeleteAsync(string itemName);
    Task ShowMessageAsync(string message, string title = "");
    Task ShowSyncConflictsAsync(SyncViewModel viewModel);
    Task<CloudFolder?> ShowCloudFolderPickerAsync(CloudFolderPickerViewModel viewModel);
}
