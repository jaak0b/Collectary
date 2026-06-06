using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.ViewModels;

public class EditorLevel : System.ComponentModel.INotifyPropertyChanged
{
    public string Title { get; }
    public ObservableCollection<IEditorNode> Rows { get; }
    public ObservableCollection<IEditorNode> OwnerRootRows { get; }
    public Guid? ScopeGroupId { get; }
    public bool SupportsGroups { get; }
    public IEditorNode? OwnerNode { get; }
    private bool _isCurrent;
    public bool IsCurrent
    {
        get => _isCurrent;
        set { _isCurrent = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsCurrent))); }
    }
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    public EditorLevel(
        string title,
        ObservableCollection<IEditorNode> rows,
        ObservableCollection<IEditorNode> ownerRootRows,
        Guid? scopeGroupId,
        bool supportsGroups,
        bool isCurrent = false,
        IEditorNode? ownerNode = null)
    {
        Title = title;
        Rows = rows;
        OwnerRootRows = ownerRootRows;
        ScopeGroupId = scopeGroupId;
        SupportsGroups = supportsGroups;
        _isCurrent = isCurrent;
        OwnerNode = ownerNode;
    }
}

public abstract partial class FieldListEditorViewModel : ViewModelBase
{
    public ObservableCollection<IEditorNode> CurrentRows { get; } = new();

    private ObservableCollection<IEditorNode>? _activeBacking;
    private bool _suppressMirror;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedFieldRow))]
    [NotifyPropertyChangedFor(nameof(SelectedGroupRow))]
    [NotifyPropertyChangedFor(nameof(IsMasterPanelVisible))]
    [NotifyPropertyChangedFor(nameof(IsDetailPanelVisible))]
    public partial IEditorNode? SelectedNode { get; set; }

    public FieldDefinitionRowViewModel? SelectedFieldRow => SelectedNode as FieldDefinitionRowViewModel;
    public FieldGroupRowViewModel? SelectedGroupRow => SelectedNode as FieldGroupRowViewModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMasterPanelVisible))]
    [NotifyPropertyChangedFor(nameof(IsDetailPanelVisible))]
    public partial bool IsNarrow { get; set; }

    public bool IsMasterPanelVisible => !IsNarrow || SelectedNode == null;
    public bool IsDetailPanelVisible => !IsNarrow || SelectedNode != null;

    [RelayCommand]
    private void MobileNavigateBack()
    {
        if (Levels.Count > 1)
            NavigateToLevel(Levels[^2]);
        else
            SelectedNode = null;
    }

    [ObservableProperty]
    public partial bool CurrentLevelSupportsGroups { get; set; }

    public ObservableCollection<EditorLevel> Levels { get; } = new();

    public ObservableCollection<EditorLevel> DrillBreadcrumbs { get; } = new();

    public IReadOnlyList<EditorLevel> VisibleDrillBreadcrumbs { get; private set; } = System.Array.Empty<EditorLevel>();

    public IReadOnlyList<EditorLevel> CollapsedDrillBreadcrumbs { get; private set; } = System.Array.Empty<EditorLevel>();

    public bool HasCollapsedDrillBreadcrumbs => CollapsedDrillBreadcrumbs.Count > 0;

    public double DrillBreadcrumbMaxWidth => IsNarrow ? 140 : 400;

    private int MaxVisibleDrillBreadcrumbs => IsNarrow ? 1 : 2;

    public bool IsNested => Levels.Count > 1;

    protected FieldListEditorViewModel()
    {
        Levels.CollectionChanged += (_, _) => RebuildDrillBreadcrumbs();
    }

    private void RebuildDrillBreadcrumbs()
    {
        DrillBreadcrumbs.Clear();
        for (var i = 1; i < Levels.Count; i++)
            DrillBreadcrumbs.Add(Levels[i]);
        RebuildCollapsedDrillBreadcrumbs();
    }

    private void RebuildCollapsedDrillBreadcrumbs()
    {
        var trail = new BreadcrumbTrail<EditorLevel>(DrillBreadcrumbs, MaxVisibleDrillBreadcrumbs);
        VisibleDrillBreadcrumbs = trail.Visible;
        CollapsedDrillBreadcrumbs = trail.Collapsed;
        OnPropertyChanged(nameof(VisibleDrillBreadcrumbs));
        OnPropertyChanged(nameof(CollapsedDrillBreadcrumbs));
        OnPropertyChanged(nameof(HasCollapsedDrillBreadcrumbs));
    }

    partial void OnIsNarrowChanged(bool value)
    {
        RebuildCollapsedDrillBreadcrumbs();
        OnPropertyChanged(nameof(DrillBreadcrumbMaxWidth));
    }

    public void ResetToRoot()
    {
        if (Levels.Count > 1) NavigateToLevel(Levels[0]);
    }

    protected bool NavigateUpOneLevel()
    {
        if (Levels.Count <= 1) return false;
        NavigateToLevel(Levels[^2]);
        return true;
    }

    protected void InitRoot(
        string title,
        ObservableCollection<IEditorNode> rootRows,
        bool supportsGroups)
    {
        CurrentRows.CollectionChanged -= MirrorToBacking;
        CurrentRows.CollectionChanged += MirrorToBacking;
        Levels.Clear();
        Levels.Add(new EditorLevel(title, rootRows, rootRows, scopeGroupId: null, supportsGroups, isCurrent: true));
        SetCurrentLevel(Levels[0]);
    }

    private void SetCurrentLevel(EditorLevel level)
    {
        _suppressMirror = true;
        CurrentRows.Clear();
        foreach (var row in level.Rows) CurrentRows.Add(row);
        _suppressMirror = false;
        _activeBacking = level.Rows;

        CurrentLevelSupportsGroups = level.SupportsGroups;
        OnPropertyChanged(nameof(IsNested));
        PopulateFieldRowGroups(level);
    }

    private void MirrorToBacking(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_suppressMirror || _activeBacking is null) return;
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (var i = 0; i < e.NewItems!.Count; i++)
                    _activeBacking.Insert(e.NewStartingIndex + i, (IEditorNode)e.NewItems[i]!);
                break;
            case NotifyCollectionChangedAction.Remove:
                for (var i = 0; i < e.OldItems!.Count; i++)
                    _activeBacking.RemoveAt(e.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Move:
                _activeBacking.Move(e.OldStartingIndex, e.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Replace:
                _activeBacking[e.NewStartingIndex] = (IEditorNode)e.NewItems![0]!;
                break;
            case NotifyCollectionChangedAction.Reset:
                _activeBacking.Clear();
                break;
        }
    }

    protected void PopulateCurrentLevelGroups() => PopulateFieldRowGroups(Levels[^1]);

    protected void RefreshCurrentLevel() => SetCurrentLevel(Levels[^1]);

    private void PopulateFieldRowGroups(EditorLevel level)
    {
        var ownerGroups = CollectGroups(level.OwnerRootRows).ToList();
        var levelColumnCount = ColumnCountForLevel(level);
        foreach (var node in level.Rows)
        {
            if (node is not FieldDefinitionRowViewModel field) continue;
            SyncAvailableGroups(field.AvailableGroups, ownerGroups);
            field.AssignedGroupId = level.ScopeGroupId;
            field.GroupMoveRequested = (row, target) => MoveFieldToGroup(level, row, target);
            field.SetParentColumnCount(levelColumnCount);
        }
    }

    private int ColumnCountForLevel(EditorLevel level) => level.OwnerNode switch
    {
        FieldGroupRowViewModel group => group.ColumnCount,
        FieldDefinitionRowViewModel listField => listField.ColumnCount,
        _ => GetRootColumnCount()
    };

    protected virtual int GetRootColumnCount() => 1;

    private static void SyncAvailableGroups(
        ObservableCollection<FieldGroupRowViewModel> target,
        List<FieldGroupRowViewModel> desired)
    {
        if (target.SequenceEqual(desired)) return;

        for (var i = target.Count - 1; i >= 0; i--)
            if (!desired.Contains(target[i])) target.RemoveAt(i);

        for (var i = 0; i < desired.Count; i++)
        {
            var existing = target.IndexOf(desired[i]);
            if (existing < 0) target.Insert(i, desired[i]);
            else if (existing != i) target.Move(existing, i);
        }
    }

    private void MoveFieldToGroup(EditorLevel level, FieldDefinitionRowViewModel row, FieldGroupRowViewModel? target)
    {
        if (target?.Id == level.ScopeGroupId) return;
        if (ReferenceEquals(SelectedNode, row)) SelectedNode = null;
        CurrentRows.Remove(row);
        var destination = target?.ChildNodes ?? level.OwnerRootRows;
        row.DisplayOrder = destination.Count;
        destination.Add(row);
        row.AssignedGroupId = target?.Id;
        row.ListColumnSuppressed = target is not null && !target.EffectiveListAllowed;
    }

    private static IEnumerable<FieldGroupRowViewModel> CollectGroups(IEnumerable<IEditorNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node is FieldGroupRowViewModel group)
            {
                yield return group;
                foreach (var nested in CollectGroups(group.ChildNodes)) yield return nested;
            }
        }
    }

    protected virtual Task AddField(FieldDefinition definition)
    {
        definition.DisplayOrder = CurrentRows.Count;
        var row = new FieldDefinitionRowViewModel(definition);
        CurrentRows.Add(row);
        PopulateFieldRowGroups(Levels[^1]);
        SelectedNode = row;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void AddGroup()
    {
        if (!CurrentLevelSupportsGroups) return;
        var group = new FieldGroupRowViewModel(LocalizationService.Instance["NewGroup"])
        {
            DisplayOrder = CurrentRows.Count
        };
        CurrentRows.Add(group);
        SelectedNode = group;
    }

    /// <summary>
    /// The single source of truth for the "Add field" menu, shared by every editor that derives from this
    /// base (preset editor and system-field library). Both menus render these entries, so they can never
    /// diverge, and a new field type appears automatically — no menu edits required.
    /// </summary>
    public IReadOnlyList<FieldTypeCatalogEntry> AddableFieldTypes { get; } = new FieldTypeCatalog().Entries;

    [RelayCommand]
    private Task AddFieldOfType(FieldTypeCatalogEntry entry) => AddField(entry.Create());

    [RelayCommand]
    protected virtual Task RemoveField(IEditorNode node)
    {
        if (node is FieldDefinitionRowViewModel { IsDisplayName: true }) return Task.CompletedTask;
        if (ReferenceEquals(SelectedNode, node)) SelectedNode = null;

        if (node is FieldGroupRowViewModel group)
        {
            var rehomed = CollectFields(group.ChildNodes).ToList();
            CurrentRows.Remove(group);
            foreach (var field in rehomed)
            {
                field.AssignedGroupId = Levels[^1].ScopeGroupId;
                field.DisplayOrder = CurrentRows.Count;
                field.ListColumnSuppressed = false;
                CurrentRows.Add(field);
            }
            PopulateFieldRowGroups(Levels[^1]);
        }
        else
        {
            CurrentRows.Remove(node);
        }
        return Task.CompletedTask;
    }

    private static IEnumerable<FieldDefinitionRowViewModel> CollectFields(IEnumerable<IEditorNode> nodes)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case FieldDefinitionRowViewModel field:
                    yield return field;
                    break;
                case FieldGroupRowViewModel group:
                    foreach (var nested in CollectFields(group.ChildNodes)) yield return nested;
                    break;
            }
        }
    }

    public void MoveField(int from, int to)
    {
        if (from < 0 || to < 0 || from >= CurrentRows.Count || to >= CurrentRows.Count) return;
        if (from == to) return;
        CurrentRows.Move(from, to);
    }

    [RelayCommand]
    private void DrillInto(IEditorNode node)
    {
        if (!node.IsDrillable) return;
        var parent = Levels[^1];
        parent.IsCurrent = false;

        var ownerRoot = node.IsGroupNode ? parent.OwnerRootRows : node.DrillChildren;
        var scope = node.IsGroupNode ? ((FieldGroupRowViewModel)node).Id : (Guid?)null;

        Levels.Add(new EditorLevel(
            node.DisplayLabel, node.DrillChildren, ownerRoot, scope,
            supportsGroups: true, isCurrent: true, ownerNode: node));
        SelectedNode = null;
        SetCurrentLevel(Levels[^1]);
    }

    [RelayCommand]
    private void NavigateToLevel(EditorLevel level)
    {
        var index = Levels.IndexOf(level);
        if (index < 0) return;
        while (Levels.Count > index + 1)
            Levels.RemoveAt(Levels.Count - 1);
        Levels[^1].IsCurrent = true;
        SelectedNode = null;
        SetCurrentLevel(Levels[^1]);
    }
}
