using System.Collections.ObjectModel;

namespace Collectary.UI.ViewModels;

public interface IGroupedFieldHost
{
    int UngroupedColumnCount { get; }
    ObservableCollection<FieldEditorViewModelBase> UngroupedEditors { get; }
    ObservableCollection<ViewModelBase> LayoutRegions { get; }
}
