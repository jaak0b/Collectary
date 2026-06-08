using System.Collections.ObjectModel;

namespace Collectary.Presentation.ViewModels;

public interface IGroupedFieldHost
{
    int UngroupedColumnCount { get; }
    double FieldMinColumnWidth { get; }
    ObservableCollection<FieldEditorViewModelBase> UngroupedEditors { get; }
    ObservableCollection<ViewModelBase> LayoutRegions { get; }
}
