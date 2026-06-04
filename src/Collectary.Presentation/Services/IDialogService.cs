using Collectary.Presentation.ViewModels;

namespace Collectary.Presentation.Services;

public interface IDialogService
{
    Task<bool> ConfirmDeleteAsync(string itemName);
    Task ShowMessageAsync(string message, string title = "");
    Task ShowShareDialogAsync(ShareDialogViewModel viewModel);
}
