using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;

namespace Collectary.Presentation.Services;

public partial class OverlayDialogService : ObservableObject, IDialogService, IDialogHost
{
    private readonly Queue<DialogEntry> _queue = new();

    [ObservableProperty]
    public partial ViewModelBase? ActiveDialog { get; private set; }

    public bool HasActiveDialog => ActiveDialog is not null;

    partial void OnActiveDialogChanged(ViewModelBase? value) => OnPropertyChanged(nameof(HasActiveDialog));

    public async Task<bool> ConfirmDeleteAsync(string itemName)
    {
        var loc = LocalizationService.Instance;
        var vm = new ConfirmDialogViewModel(
            message: string.Format(loc["ConfirmDeleteBody"], itemName),
            confirmLabel: loc["Delete"],
            cancelLabel: loc["Cancel"],
            title: loc["ConfirmDeleteTitle"]);
        var entry = Enqueue(vm);
        vm.Closed += result => Complete(entry, result);
        return await entry.Result.Task is true;
    }

    public async Task<bool> ConfirmAsync(string message, string confirmLabel, string title)
    {
        var vm = new ConfirmDialogViewModel(
            message: message,
            confirmLabel: confirmLabel,
            cancelLabel: LocalizationService.Instance["Cancel"],
            title: title);
        var entry = Enqueue(vm);
        vm.Closed += result => Complete(entry, result);
        return await entry.Result.Task is true;
    }

    public async Task ShowMessageAsync(string message, string title = "")
    {
        var effectiveTitle = string.IsNullOrEmpty(title)
            ? LocalizationService.Instance["AppTitle"]
            : title;
        var vm = new MessageDialogViewModel(message, effectiveTitle);
        var entry = Enqueue(vm);
        vm.Closed += result => Complete(entry, result);
        await entry.Result.Task;
    }

    public async Task<CloudFolder?> ShowCloudFolderPickerAsync(CloudFolderPickerViewModel viewModel)
    {
        await viewModel.InitializeAsync();
        var entry = Enqueue(viewModel);
        viewModel.CloseRequested = folder => Complete(entry, folder);
        return await entry.Result.Task as CloudFolder;
    }

    private DialogEntry Enqueue(ViewModelBase viewModel)
    {
        var entry = new DialogEntry(viewModel);
        _queue.Enqueue(entry);
        if (_queue.Count == 1)
            ActiveDialog = viewModel;
        return entry;
    }

    private void Complete(DialogEntry entry, object? result)
    {
        if (_queue.Count == 0 || _queue.Peek() != entry) return;
        _queue.Dequeue();
        ActiveDialog = _queue.Count > 0 ? _queue.Peek().ViewModel : null;
        entry.Result.TrySetResult(result);
    }

    private sealed class DialogEntry
    {
        public DialogEntry(ViewModelBase viewModel) => ViewModel = viewModel;

        public ViewModelBase ViewModel { get; }

        public TaskCompletionSource<object?> Result { get; } = new();
    }
}
