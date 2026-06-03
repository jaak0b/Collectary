using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.UI.Localization;

namespace Collectary.UI.ViewModels;

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
    public partial IEditorNode? SelectedNode { get; set; }

    public FieldDefinitionRowViewModel? SelectedFieldRow => SelectedNode as FieldDefinitionRowViewModel;
    public FieldGroupRowViewModel? SelectedGroupRow => SelectedNode as FieldGroupRowViewModel;

    [ObservableProperty]
    public partial bool CurrentLevelSupportsGroups { get; set; }

    public ObservableCollection<EditorLevel> Levels { get; } = new();

    public bool IsNested => Levels.Count > 1;

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
        foreach (var node in level.Rows)
        {
            if (node is not FieldDefinitionRowViewModel field) continue;
            SyncAvailableGroups(field.AvailableGroups, ownerGroups);
            field.AssignedGroupId = level.ScopeGroupId;
            field.GroupMoveRequested = (row, target) => MoveFieldToGroup(level, row, target);
        }
    }

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

    [RelayCommand]
    private async Task AddTextField() => await AddField(new TextFieldDefinition { Label = LocalizationService.Instance["NewTextField"] });

    [RelayCommand]
    private async Task AddBoolField() => await AddField(new BoolFieldDefinition { Label = LocalizationService.Instance["NewBoolField"] });

    [RelayCommand]
    private async Task AddIntegerField() => await AddField(new IntegerFieldDefinition { Label = LocalizationService.Instance["NewNumberField"] });

    [RelayCommand]
    private async Task AddDecimalField() => await AddField(new DecimalFieldDefinition { Label = LocalizationService.Instance["NewDecimalField"] });

    [RelayCommand]
    private async Task AddDateField() => await AddField(new DateFieldDefinition { Label = LocalizationService.Instance["NewDateField"] });

    [RelayCommand]
    private async Task AddColorField() => await AddField(new ColorFieldDefinition { Label = LocalizationService.Instance["NewColorField"] });

    [RelayCommand]
    private async Task AddRatingField() => await AddField(new RatingFieldDefinition { Label = LocalizationService.Instance["NewRatingField"] });

    [RelayCommand]
    private async Task AddUrlField() => await AddField(new UrlFieldDefinition { Label = LocalizationService.Instance["NewUrlField"] });

    [RelayCommand]
    private async Task AddSingleChoiceField() => await AddField(new SingleChoiceFieldDefinition { Label = LocalizationService.Instance["NewSingleChoiceField"] });

    [RelayCommand]
    private async Task AddMultiChoiceField() => await AddField(new MultiChoiceFieldDefinition { Label = LocalizationService.Instance["NewMultiChoiceField"] });

    [RelayCommand]
    private async Task AddListField() => await AddField(new ListFieldDefinition { Label = LocalizationService.Instance["NewListField"] });

    [RelayCommand]
    private async Task AddImageField() => await AddField(new ImageFieldDefinition { Label = LocalizationService.Instance["NewImageField"] });

    [RelayCommand]
    private async Task AddRichTextField() => await AddField(new RichTextFieldDefinition { Label = LocalizationService.Instance["NewRichTextField"] });

    [RelayCommand]
    private async Task AddPhoneField() => await AddField(new PhoneFieldDefinition { Label = LocalizationService.Instance["NewPhoneField"] });

    [RelayCommand]
    private async Task AddEmailField() => await AddField(new EmailFieldDefinition { Label = LocalizationService.Instance["NewEmailField"] });

    [RelayCommand]
    private async Task AddPercentageField() => await AddField(new PercentageFieldDefinition { Label = LocalizationService.Instance["NewPercentageField"] });

    [RelayCommand]
    private async Task AddDurationField() => await AddField(new DurationFieldDefinition { Label = LocalizationService.Instance["NewDurationField"] });

    [RelayCommand]
    private async Task AddTimeField() => await AddField(new TimeFieldDefinition { Label = LocalizationService.Instance["NewTimeField"] });

    [RelayCommand]
    private async Task AddCurrencyField() => await AddField(new CurrencyFieldDefinition { Label = LocalizationService.Instance["NewCurrencyField"] });

    [RelayCommand]
    private async Task AddTagsField() => await AddField(new TagsFieldDefinition { Label = LocalizationService.Instance["NewTagsField"] });

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
