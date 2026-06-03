using System.Collections.ObjectModel;

namespace Collectary.UI.ViewModels;

public interface IGroupedFieldHost
{
    ObservableCollection<FieldEditorViewModelBase> UngroupedEditors { get; }
    ObservableCollection<ViewModelBase> LayoutRegions { get; }
}
