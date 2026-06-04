using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Ports;

namespace Collectary.Presentation.ViewModels;

/// <summary>
/// Lets the user browse their cloud folders (starting at the provider root), create subfolders, and
/// pick one as the sync root. Backed by <see cref="ICloudFileStore"/>; closes via
/// <see cref="CloseRequested"/> with the chosen folder, or null when cancelled.
/// </summary>
public partial class CloudFolderPickerViewModel : ViewModelBase
{
    private readonly ICloudFileStore _store;
    private readonly Stack<CloudFolder> _ancestors = new();

    public CloudFolderPickerViewModel(ICloudFileStore store, CloudFolder root)
    {
        _store = store;
        CurrentFolder = root;
    }

    public Action<CloudFolder?>? CloseRequested { get; set; }

    public ObservableCollection<CloudFolder> Subfolders { get; } = new();

    [ObservableProperty]
    public partial CloudFolder CurrentFolder { get; set; }

    [ObservableProperty]
    public partial string NewFolderName { get; set; } = string.Empty;

    public bool CanGoUp => _ancestors.Count > 0;

    public async Task InitializeAsync() => await ReloadAsync();

    [RelayCommand]
    private async Task OpenFolder(CloudFolder folder)
    {
        _ancestors.Push(CurrentFolder);
        CurrentFolder = folder;
        OnPropertyChanged(nameof(CanGoUp));
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task GoUp()
    {
        if (_ancestors.Count == 0) return;
        CurrentFolder = _ancestors.Pop();
        OnPropertyChanged(nameof(CanGoUp));
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task CreateFolder()
    {
        var name = NewFolderName?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        await _store.EnsureFolderAsync(CurrentFolder.Id, name, CancellationToken.None);
        NewFolderName = string.Empty;
        await ReloadAsync();
    }

    [RelayCommand]
    private void Select() => CloseRequested?.Invoke(CurrentFolder);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(null);

    private async Task ReloadAsync()
    {
        var folders = await _store.ListFoldersAsync(CurrentFolder.Id, CancellationToken.None);
        Subfolders.Clear();
        foreach (var folder in folders)
            Subfolders.Add(folder);
    }
}
