using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.UI.Localization;

namespace Collectary.UI.ViewModels;

public partial class FieldDefinitionRowViewModel : ViewModelBase, IEditorNode
{
    private readonly FieldDefinition _definition;
    public bool IsSystemField { get; }
    public bool IsEditable => !IsSystemField;
    public Guid? SystemFieldOwnerId => _definition.SystemFieldId;

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
    public partial bool IsShownInList { get; set; }

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
                OnPropertyChanged(nameof(ColumnSpanOptions));
                OnPropertyChanged(nameof(IsInMultiColumnContext));
                if (ColumnSpan > EffectiveColumnCount) ColumnSpan = EffectiveColumnCount;
                return;
            }
            if (value.Id == AssignedGroupId) return;
            if (GroupMoveRequested is { } move) move(this, value);
            else AssignedGroupId = value.Id;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ColumnSpanOptions));
            OnPropertyChanged(nameof(IsInMultiColumnContext));
            if (ColumnSpan > EffectiveColumnCount) ColumnSpan = EffectiveColumnCount;
        }
    }

    public bool HasAvailableGroups => AvailableGroups.Count > 0;

    public string TypeDisplayName => _definition.GetType().ToLocalizedString();
    public string TypeIcon => _definition.GetType().GetFieldIcon();
    public int SubFieldCount => SubFieldRows.Count;
    public bool CanShowInList => _definition is IListDisplayable;
    public bool HasChoices => _definition is SingleChoiceFieldDefinition or MultiChoiceFieldDefinition;
    public bool IsDisplayName => _definition is DisplayNameFieldDefinition;
    public bool CanDelete => !IsDisplayName;
    public bool IsLabelEditable => !IsDisplayName;
    public string DisplayLabel => IsDisplayName
        ? _definition.GetType().ToLocalizedString()
        : IsSystemField ? $"🔒 {Label}" : Label;
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
    public partial ColorFormat ColorFormat { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGridInline))]
    public partial ListInlineStyle InlineStyle { get; set; }

    [ObservableProperty]
    public partial int DisplayWidth { get; set; }

    [ObservableProperty]
    public partial int DisplayHeight { get; set; }

    public IReadOnlyList<ImageSizeMode> ImageSizeModes { get; } = Enum.GetValues<ImageSizeMode>();

    [ObservableProperty]
    public partial ImageSizeMode ImageSizeMode { get; set; }

    [ObservableProperty]
    public partial string CurrencySymbol { get; set; } = "€";

    [ObservableProperty]
    public partial int ColumnSpan { get; set; } = 1;

    [ObservableProperty]
    public partial int ListColumnCount { get; set; } = 1;

    private int _parentColumnCount = 1;

    private int EffectiveColumnCount => SelectedGroup?.ColumnCount ?? _parentColumnCount;

    public IReadOnlyList<int> ColumnSpanOptions =>
        Enumerable.Range(1, EffectiveColumnCount).ToList();

    public bool IsInMultiColumnContext => EffectiveColumnCount > 1;

    public void SetParentColumnCount(int count)
    {
        _parentColumnCount = count;
        OnPropertyChanged(nameof(ColumnSpanOptions));
        OnPropertyChanged(nameof(IsInMultiColumnContext));
        if (ColumnSpan > EffectiveColumnCount) ColumnSpan = EffectiveColumnCount;
    }

    internal void NotifyColumnSpanOptionsChanged()
    {
        OnPropertyChanged(nameof(ColumnSpanOptions));
        OnPropertyChanged(nameof(IsInMultiColumnContext));
    }

    [ObservableProperty]
    public partial int MaxStars { get; set; } = 5;

    public FieldDefinitionRowViewModel(
        FieldDefinition definition,
        bool isSystemField = false)
    {
        _definition = definition;
        IsSystemField = isSystemField;
        AssignedGroupId = definition.GroupId;
        Label = definition.Label;
        IsRequired = definition.IsRequired;
        IsShownInList = (_definition as IListDisplayable)?.ShowInList ?? false;
        ColorFormat = (_definition as ColorFieldDefinition)?.Format ?? ColorFormat.Hex;
        InlineStyle = (_definition as ListFieldDefinition)?.InlineStyle ?? ListInlineStyle.Card;
        DisplayWidth = (_definition as ImageFieldDefinition)?.DisplayWidth ?? 200;
        DisplayHeight = (_definition as ImageFieldDefinition)?.DisplayHeight ?? 200;
        ImageSizeMode = (_definition as ImageFieldDefinition)?.SizeMode ?? ImageSizeMode.Fixed;
        CurrencySymbol = (_definition as CurrencyFieldDefinition)?.CurrencySymbol ?? "€";
        ColumnSpan = definition.ColumnSpan;
        MaxStars = (_definition as RatingFieldDefinition)?.MaxStars ?? 5;
        ListColumnCount = (_definition as ListFieldDefinition)?.ColumnCount ?? 1;

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
            foreach (var root in groupNodes.Where(g => g.ParentGroupId is null))
                root.ApplyListGate(true);
        }
    }

    partial void OnListColumnCountChanged(int value)
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

    public FieldDefinition BuildDefinition()
    {
        if (IsSystemField) return _definition;
        if (!IsDisplayName)
            _definition.Label = Label;
        _definition.IsRequired = IsRequired;
        _definition.ColumnSpan = ColumnSpan;
        _definition.GroupId = IsDisplayName ? null : AssignedGroupId;
        if (_definition is IListDisplayable ld)
            ld.ShowInList = IsShownInList;
        if (_definition is ColorFieldDefinition cd)
            cd.Format = ColorFormat;
        if (_definition is ImageFieldDefinition imgDef)
        {
            imgDef.DisplayWidth = DisplayWidth;
            imgDef.DisplayHeight = DisplayHeight;
            imgDef.SizeMode = ImageSizeMode;
        }
        if (_definition is CurrencyFieldDefinition currDef)
            currDef.CurrencySymbol = CurrencySymbol;
        if (_definition is RatingFieldDefinition ratingDef)
            ratingDef.MaxStars = MaxStars;
        if (_definition is ListFieldDefinition listDef)
            listDef.ColumnCount = ListColumnCount;

        var options = ChoiceItems
            .Select((item, index) => new ChoiceOption { Value = item.Value, DisplayOrder = index })
            .ToList();

        if (_definition is SingleChoiceFieldDefinition sc)
            sc.Choices = options;
        else if (_definition is MultiChoiceFieldDefinition mc)
            mc.Choices = options;
        else if (_definition is ListFieldDefinition lfd)
        {
            lfd.InlineStyle = InlineStyle;
            var flat = new EditorNodeTreeBuilder().Flatten(SubFieldRows);
            lfd.Groups = flat.Groups
                .Select(g =>
                {
                    var built = g.Build(g.DisplayOrder);
                    built.PresetId = null;
                    built.ParentListFieldDefinitionId = lfd.Id;
                    return built;
                })
                .ToList();
            lfd.SubFields = flat.Fields
                .Select(row =>
                {
                    var sub = row.BuildDefinition();
                    sub.DisplayOrder = row.DisplayOrder;
                    sub.ParentListFieldDefinitionId = lfd.Id;
                    return sub;
                })
                .ToList();
        }

        return _definition;
    }

}
