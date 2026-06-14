using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Collectary.Search.ViewModels;

public partial class ChipValueOption : ObservableObject
{
    private readonly Action _onChanged;

    public string Value { get; }

    [ObservableProperty]
    public partial bool IsChecked { get; set; }

    public ChipValueOption(string value, Action onChanged)
    {
        Value = value;
        _onChanged = onChanged;
    }

    partial void OnIsCheckedChanged(bool value) => _onChanged();
}

public partial class FilterChipViewModel : ObservableObject
{
    private readonly ILocalizationProvider _localization;
    private readonly Action _onChanged;
    private readonly Action? _onRemoveRequested;
    private readonly List<ChipValueOption> _allOptions = new();
    private bool _suppressNotifications;

    public string Label { get; }
    public bool IsChoiceStyle { get; }
    public QueryOperatorKind TextOperator { get; }
    public ObservableCollection<ChipValueOption> VisibleOptions { get; } = new();

    [ObservableProperty]
    public partial string ValueSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FreeText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsFlyoutOpen { get; set; }

    public FilterChipViewModel(
        string label,
        IReadOnlyList<string> suggestions,
        QueryOperatorKind textOperator,
        ILocalizationProvider localization,
        Action onChanged,
        Action? onRemoveRequested = null)
    {
        Label = label;
        IsChoiceStyle = suggestions.Count > 0;
        TextOperator = textOperator;
        _localization = localization;
        _onChanged = onChanged;
        _onRemoveRequested = onRemoveRequested;
        _suppressNotifications = true;
        foreach (var suggestion in suggestions)
            _allOptions.Add(new ChipValueOption(suggestion, OnSelectionChanged));
        RefreshVisibleOptions();
        _suppressNotifications = false;
    }

    public bool HasSelection => IsChoiceStyle
        ? _allOptions.Any(o => o.IsChecked)
        : !string.IsNullOrWhiteSpace(FreeText);

    public string DisplayText
    {
        get
        {
            var selected = IsChoiceStyle
                ? _allOptions.Where(o => o.IsChecked).Select(o => o.Value).ToList()
                : string.IsNullOrWhiteSpace(FreeText) ? [] : new List<string> { FreeText.Trim() };
            var summary = selected.Count switch
            {
                0 => _localization.Get(SearchLocalizationKeys.SearchAllValues),
                1 => selected[0],
                _ => string.Format(_localization.Get(SearchLocalizationKeys.SearchSelectedCount), selected.Count),
            };
            return Label + ": " + summary;
        }
    }

    public BasicConditionRow? ToRow()
    {
        if (IsChoiceStyle)
        {
            var values = _allOptions.Where(o => o.IsChecked).Select(o => o.Value).ToList();
            return values.Count switch
            {
                0 => null,
                1 => new BasicConditionRow(Label, QueryOperatorKind.Equals, values),
                _ => new BasicConditionRow(Label, QueryOperatorKind.In, values),
            };
        }
        return string.IsNullOrWhiteSpace(FreeText)
            ? null
            : new BasicConditionRow(Label, TextOperator, [FreeText.Trim()]);
    }

    public void ApplyValues(IReadOnlyList<string> values)
    {
        _suppressNotifications = true;
        if (IsChoiceStyle)
        {
            foreach (var value in values)
            {
                var option = _allOptions.FirstOrDefault(o =>
                    string.Equals(o.Value, value, StringComparison.OrdinalIgnoreCase));
                if (option is null)
                {
                    option = new ChipValueOption(value, OnSelectionChanged);
                    _allOptions.Add(option);
                }
                option.IsChecked = true;
            }
            RefreshVisibleOptions();
        }
        else
        {
            FreeText = values.Count > 0 ? values[0] : string.Empty;
        }
        _suppressNotifications = false;
        RaiseSelectionProperties();
    }

    public string OperatorHint => _localization.Get(
        TextOperator == QueryOperatorKind.Contains ? SearchLocalizationKeys.SearchContainsLabel : SearchLocalizationKeys.SearchEqualsLabel);

    public string ValueSearchPlaceholder => _localization.Get(SearchLocalizationKeys.SearchFindValues);
    public string ValuePlaceholder => _localization.Get(SearchLocalizationKeys.SearchValuePlaceholder);
    public string ClearLabel => _localization.Get(SearchLocalizationKeys.SearchClear);
    public string RemoveLabel => _localization.Get(SearchLocalizationKeys.SearchRemoveFilter);

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(OperatorHint));
        OnPropertyChanged(nameof(ValueSearchPlaceholder));
        OnPropertyChanged(nameof(ValuePlaceholder));
        OnPropertyChanged(nameof(ClearLabel));
        OnPropertyChanged(nameof(RemoveLabel));
    }

    [RelayCommand]
    private void Remove() => _onRemoveRequested?.Invoke();

    [RelayCommand]
    private void ClearValues()
    {
        _suppressNotifications = true;
        foreach (var option in _allOptions)
            option.IsChecked = false;
        FreeText = string.Empty;
        _suppressNotifications = false;
        OnSelectionChanged();
    }

    partial void OnValueSearchTextChanged(string value) => RefreshVisibleOptions();

    partial void OnFreeTextChanged(string value) => OnSelectionChanged();

    private void OnSelectionChanged()
    {
        if (_suppressNotifications) return;
        RaiseSelectionProperties();
        _onChanged();
    }

    private void RaiseSelectionProperties()
    {
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(HasSelection));
    }

    private void RefreshVisibleOptions()
    {
        var matching = _allOptions.Where(o =>
            o.IsChecked
            || o.Value.Contains(ValueSearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        VisibleOptions.Clear();
        foreach (var option in matching)
            VisibleOptions.Add(option);
    }
}
