using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels;

public partial class PresetRowViewModel : ViewModelBase
{
    public Preset Preset { get; }
    public int ItemCount { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public IRelayCommand NavigateCommand { get; }
    public IRelayCommand EditCommand { get; }
    public IAsyncRelayCommand DeleteCommand { get; }
    public IRelayCommand ShareCommand { get; }

    public PresetRowViewModel(Preset preset, int itemCount, Action onNavigate, Action onEdit, Func<Task> onDelete, Action? onShare = null)
    {
        Preset = preset;
        ItemCount = itemCount;
        NavigateCommand = new RelayCommand(onNavigate);
        EditCommand = new RelayCommand(onEdit);
        DeleteCommand = new AsyncRelayCommand(onDelete);
        ShareCommand = new RelayCommand(onShare ?? (() => { }));
    }
}
