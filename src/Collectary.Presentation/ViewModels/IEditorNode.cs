using System.Collections.ObjectModel;

namespace Collectary.UI.ViewModels;

public interface IEditorNode
{
    bool IsGroupNode { get; }
    bool IsDrillable { get; }
    bool CanDelete { get; }
    string DisplayLabel { get; }
    string TypeIcon { get; }
    int DisplayOrder { get; set; }
    ObservableCollection<IEditorNode> DrillChildren { get; }
}
