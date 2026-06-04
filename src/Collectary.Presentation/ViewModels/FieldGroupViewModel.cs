using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class FieldGroupViewModel : ViewModelBase
{
    public string Name { get; }
    public GroupDisplayMode DisplayMode { get; }
    public int ColumnCount { get; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    public ObservableCollection<FieldEditorViewModelBase> Editors { get; } = new();

    public ObservableCollection<ViewModelBase> ChildRegions { get; } = new();

    public bool HasVisibleContent => Editors.Count > 0 || ChildRegions.Count > 0;

    public FieldGroupViewModel(FieldGroup group)
    {
        Name = group.Name;
        DisplayMode = group.DisplayMode;
        ColumnCount = group.ColumnCount;
        IsExpanded = !group.DefaultCollapsed;
    }
}
