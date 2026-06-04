using System.Collections.ObjectModel;

namespace Collectary.Presentation.ViewModels;

public class TabRegionViewModel : ViewModelBase
{
    public ItemEditingContext Context { get; }
    public ObservableCollection<FieldGroupViewModel> TabGroups { get; } = new();

    public TabRegionViewModel(ItemEditingContext context)
    {
        Context = context;
    }
}
