using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.ViewModels;

public partial class FieldDefinitionRowViewModel : ViewModelBase, IEditorNode
{
    private readonly FieldDefinition _definition;
    internal FieldDefinition Definition => _definition;
    public Guid Id => _definition.Id;
    public bool IsSharedField { get; }
    public bool IsEditable => !IsSharedField;
    public Guid? SharedFieldOwnerId => _definition.SharedFieldId;

    public bool IsGroupNode => false;
    public bool IsDrillable => IsList;
    public ObservableCollection<IEditorNode> DrillChildren => SubFieldRows;
    public int DisplayOrder
    {
        get => _definition.DisplayOrder;
        set => _definition.DisplayOrder = value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowInListCheckboxVisible))]
    public partial bool ListColumnSuppressed { get; set; }

    public bool ShowInListCheckboxVisible => CanShowInList && !ListColumnSuppressed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayLabel))]
    public partial string Label { get; set; }

    [ObservableProperty]
    public partial bool IsRequired { get; set; }

    [ObservableProperty]
    public partial bool ShowInList { get; set; }

    public ObservableCollection<ChoiceOptionRowViewModel> ChoiceItems { get; } = new();
    public ObservableCollection<IEditorNode> SubFieldRows { get; } = new();
    public ObservableCollection<FieldGroupRowViewModel> AvailableGroups { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedGroup))]
    public partial Guid? AssignedGroupId { get; set; }

    public Action<FieldDefinitionRowViewModel, FieldGroupRowViewModel?>? GroupMoveRequested { get; set; }

    public FieldGroupRowViewModel? SelectedGroup
    {
        get => AvailableGroups.FirstOrDefault(g => g.Id == AssignedGroupId);
        set
        {
            if (value is null)
            {
                OnPropertyChanged();
                OnEffectiveColumnCountChanged();
                return;
            }
            if (value.Id == AssignedGroupId) return;
            if (GroupMoveRequested is { } move) move(this, value);
            else AssignedGroupId = value.Id;
            OnPropertyChanged();
            OnEffectiveColumnCountChanged();
        }
    }

    public bool HasAvailableGroups => AvailableGroups.Count > 0;

    public string TypeDisplayName => _definition.GetType().ToLocalizedString();
    public string TypeIcon => _definition.GetType().GetFieldIcon();
    public int SubFieldCount => SubFieldRows.Count;
    public bool CanShowInList => _definition is IListDisplayable;
    public bool HasChoices => _definition is SingleChoiceFieldDefinition or MultiChoiceFieldDefinition;
    public bool IsDisplayName => _definition.IsTitleField;
    public bool CanDelete => !IsDisplayName;
    public bool IsLabelEditable => !IsDisplayName;
    public string DisplayLabel => IsDisplayName
        ? _definition.GetType().ToLocalizedString()
        : Label;
    public string LockGlyph => IsSharedField && !IsDisplayName ? IconGlyphs.LockClosed : string.Empty;
    public bool IsColor => _definition is ColorFieldDefinition;
    public bool IsPicture => _definition is ImageFieldDefinition;
    public bool IsList => _definition is ListFieldDefinition;
    public bool IsCurrency => _definition is CurrencyFieldDefinition;
    public bool IsRating => _definition is RatingFieldDefinition;
    public bool HasTypeSettings => IsCurrency || IsColor || IsRating || IsPicture || HasChoices || IsList;
    public bool IsGridInline => IsList && InlineStyle == ListInlineStyle.Grid;
    public IReadOnlyList<ColorFormat> ColorFormats { get; } = Enum.GetValues<ColorFormat>();
    public IReadOnlyList<ListInlineStyle> InlineStyles { get; } = Enum.GetValues<ListInlineStyle>();

    [ObservableProperty]
    public partial ColorFormat Format { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGridInline))]
    public partial ListInlineStyle InlineStyle { get; set; }

    [ObservableProperty]
    public partial int DisplayWidth { get; set; }

    [ObservableProperty]
    public partial int DisplayHeight { get; set; }

    public IReadOnlyList<ImageSizeMode> ImageSizeModes { get; } = Enum.GetValues<ImageSizeMode>();

    [ObservableProperty]
    public partial ImageSizeMode SizeMode { get; set; }

    [ObservableProperty]
    public partial string CurrencySymbol { get; set; } = "€";

    [ObservableProperty]
    public partial int ColumnSpan { get; set; } = 1;

    [ObservableProperty]
    public partial int ColumnCount { get; set; } = 1;

    private int _parentColumnCount = 1;

    private int EffectiveColumnCount => SelectedGroup?.ColumnCount ?? _parentColumnCount;

    public IReadOnlyList<int> ColumnSpanOptions =>
        Enumerable.Range(1, EffectiveColumnCount).ToList();

    public bool IsInMultiColumnContext => EffectiveColumnCount > 1;

    public void SetParentColumnCount(int count)
    {
        _parentColumnCount = count;
        OnEffectiveColumnCountChanged();
    }

    private void OnEffectiveColumnCountChanged()
    {
        OnPropertyChanged(nameof(ColumnSpanOptions));
        OnPropertyChanged(nameof(IsInMultiColumnContext));
        if (ColumnSpan > EffectiveColumnCount) ColumnSpan = EffectiveColumnCount;
    }

    [ObservableProperty]
    public partial int MaxStars { get; set; } = 5;

    public FieldDefinitionRowViewModel(
        FieldDefinition definition,
        bool isSharedField = false)
    {
        _definition = definition;
        IsSharedField = isSharedField;
        AssignedGroupId = definition.GroupId;
        Label = definition.Label;
        IsRequired = definition.IsRequired;
        ShowInList = (_definition as IListDisplayable)?.ShowInList ?? false;
        Format = (_definition as ColorFieldDefinition)?.Format ?? ColorFormat.Hex;
        InlineStyle = (_definition as ListFieldDefinition)?.InlineStyle ?? ListInlineStyle.Card;
        DisplayWidth = (_definition as ImageFieldDefinition)?.DisplayWidth ?? 200;
        DisplayHeight = (_definition as ImageFieldDefinition)?.DisplayHeight ?? 200;
        SizeMode = (_definition as ImageFieldDefinition)?.SizeMode ?? ImageSizeMode.Fixed;
        CurrencySymbol = (_definition as CurrencyFieldDefinition)?.CurrencySymbol ?? "€";
        ColumnSpan = definition.ColumnSpan > 1 ? definition.ColumnSpan : definition.DefaultColumnSpan;
        MaxStars = (_definition as RatingFieldDefinition)?.MaxStars ?? 5;
        ColumnCount = (_definition as ListFieldDefinition)?.ColumnCount ?? 1;

        SubFieldRows.CollectionChanged += (_, _) => OnPropertyChanged(nameof(SubFieldCount));
        AvailableGroups.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasAvailableGroups));
            OnPropertyChanged(nameof(SelectedGroup));
        };
        LocalizationService.Instance.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(TypeDisplayName));
            OnPropertyChanged(nameof(DisplayLabel));
        };

        var existingChoices = definition switch
        {
            SingleChoiceFieldDefinition sc => sc.Choices.OrderBy(c => c.DisplayOrder).Select(c => c.Value),
            MultiChoiceFieldDefinition mc => mc.Choices.OrderBy(c => c.DisplayOrder).Select(c => c.Value),
            _ => Enumerable.Empty<string>()
        };
        foreach (var v in existingChoices)
            ChoiceItems.Add(new ChoiceOptionRowViewModel(v));

        if (definition is ListFieldDefinition lfd)
        {
            var groupNodes = lfd.Groups.Select(g => new FieldGroupRowViewModel(g)).ToList();
            var fieldRows = lfd.SubFields.Select(sub => new FieldDefinitionRowViewModel(sub)).ToList();
            foreach (var row in fieldRows.Where(r => r.AssignedGroupId == null))
                row.SetParentColumnCount(lfd.ColumnCount);
            var tree = new EditorNodeTreeBuilder().Build(groupNodes, fieldRows);
            foreach (var node in tree) SubFieldRows.Add(node);
            foreach (var group in groupNodes)
                group.RefreshChildColumnSpans();
            foreach (var root in groupNodes.Where(g => g.ParentGroupId is null))
                root.ApplyListGate(true);
        }
    }

    partial void OnColumnCountChanged(int value)
    {
        foreach (var field in SubFieldRows.OfType<FieldDefinitionRowViewModel>()
            .Where(f => f.AssignedGroupId == null))
            field.SetParentColumnCount(value);
    }

    [RelayCommand]
    private void ClearGroup()
    {
        if (AssignedGroupId is null) return;
        if (GroupMoveRequested is { } move) move(this, null);
        else AssignedGroupId = null;
        OnPropertyChanged(nameof(SelectedGroup));
    }

    [RelayCommand]
    private void AddChoice() => ChoiceItems.Add(new ChoiceOptionRowViewModel(string.Empty));

    [RelayCommand]
    private void RemoveChoice(ChoiceOptionRowViewModel item) => ChoiceItems.Remove(item);
}
