using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;

namespace Collectary.UI.ViewModels;

public partial class PresetRowViewModel : ViewModelBase
{
    public Preset Preset { get; }
    public int ItemCount { get; }

    public IRelayCommand NavigateCommand { get; }
    public IRelayCommand EditCommand { get; }
    public IAsyncRelayCommand DeleteCommand { get; }

    public PresetRowViewModel(Preset preset, int itemCount, Action onNavigate, Action onEdit, Func<Task> onDelete)
    {
        Preset = preset;
        ItemCount = itemCount;
        NavigateCommand = new RelayCommand(onNavigate);
        EditCommand = new RelayCommand(onEdit);
        DeleteCommand = new AsyncRelayCommand(onDelete);
    }
}
