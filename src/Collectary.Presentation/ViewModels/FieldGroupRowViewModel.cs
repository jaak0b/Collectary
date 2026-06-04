using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class FieldGroupRowViewModel : ViewModelBase, IEditorNode
{
    public Guid Id { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayLabel))]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial GroupDisplayMode DisplayMode { get; set; }

    [ObservableProperty]
    public partial bool DefaultCollapsed { get; set; }

    [ObservableProperty]
    public partial bool ShowInList { get; set; }

    [ObservableProperty]
    public partial bool PrefixColumnHeaders { get; set; }

    [ObservableProperty]
    public partial int ColumnCount { get; set; } = 1;

    public Guid? ParentGroupId { get; set; }

    partial void OnColumnCountChanged(int value)
    {
        foreach (var field in ChildNodes.OfType<FieldDefinitionRowViewModel>())
            field.SetParentColumnCount(value);
    }

    public int DisplayOrder { get; set; }

    public IReadOnlyList<GroupDisplayMode> DisplayModes { get; } = Enum.GetValues<GroupDisplayMode>();

    public ObservableCollection<IEditorNode> ChildNodes { get; } = new();

    public bool IsGroupNode => true;
    public bool IsDrillable => true;
    public bool CanDelete => true;
    public string DisplayLabel => Name;
    public string TypeIcon => "🗂";
    public ObservableCollection<IEditorNode> DrillChildren => ChildNodes;

    private bool _ancestorListAllowed = true;
    public bool EffectiveListAllowed => _ancestorListAllowed && ShowInList;

    public FieldGroupRowViewModel(FieldGroup group)
    {
        Id = group.Id;
        Name = group.Name;
        DisplayMode = group.DisplayMode;
        DefaultCollapsed = group.DefaultCollapsed;
        ShowInList = group.ShowInList;
        PrefixColumnHeaders = group.PrefixColumnHeaders;
        ColumnCount = group.ColumnCount;
        ParentGroupId = group.ParentGroupId;
        DisplayOrder = group.DisplayOrder;
    }

    public FieldGroupRowViewModel(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        DisplayMode = GroupDisplayMode.Card;
        ShowInList = true;
    }

    public void RefreshChildColumnSpans()
    {
        foreach (var field in ChildNodes.OfType<FieldDefinitionRowViewModel>())
            field.SetParentColumnCount(ColumnCount);
        foreach (var childGroup in ChildNodes.OfType<FieldGroupRowViewModel>())
            childGroup.RefreshChildColumnSpans();
    }

    partial void OnShowInListChanged(bool value) => ApplyListGate(_ancestorListAllowed);

    public void ApplyListGate(bool ancestorAllows)
    {
        _ancestorListAllowed = ancestorAllows;
        var effective = EffectiveListAllowed;
        foreach (var child in ChildNodes)
        {
            switch (child)
            {
                case FieldDefinitionRowViewModel field:
                    field.ListColumnSuppressed = !effective;
                    break;
                case FieldGroupRowViewModel group:
                    group.ApplyListGate(effective);
                    break;
            }
        }
    }

    public FieldGroup Build(int displayOrder) => new()
    {
        Id = Id,
        Name = Name.Trim(),
        DisplayOrder = displayOrder,
        DisplayMode = DisplayMode,
        ColumnCount = ColumnCount,
        DefaultCollapsed = DefaultCollapsed,
        ShowInList = ShowInList,
        PrefixColumnHeaders = PrefixColumnHeaders,
        ParentGroupId = ParentGroupId,
    };
}
