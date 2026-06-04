using Avalonia.Controls;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Views;

namespace Collectary.UI.Services;

public class AvaloniaDialogService : IDialogService
{
    public static AvaloniaDialogService Instance { get; } = new();

    private AvaloniaDialogService() { }

    public Window? Owner { get; set; }

    public async Task<bool> ConfirmDeleteAsync(string itemName)
    {
        if (Owner is null) return false;
        var loc = LocalizationService.Instance;
        var message = string.Format(loc["ConfirmDeleteBody"], itemName);
        var dialog = new ConfirmDialog(message);
        return await dialog.ShowDialog<bool>(Owner);
    }

    public async Task ShowMessageAsync(string message, string title = "")
    {
        if (Owner is null) return;
        var effectiveTitle = string.IsNullOrEmpty(title)
            ? LocalizationService.Instance["AppTitle"]
            : title;
        var dialog = new MessageDialog(message, effectiveTitle);
        await dialog.ShowDialog(Owner);
    }

    public async Task ShowShareDialogAsync(ShareDialogViewModel viewModel)
    {
        if (Owner is null) return;
        var dialog = new ShareDialog { DataContext = viewModel };
        await dialog.ShowDialog(Owner);
    }
}
