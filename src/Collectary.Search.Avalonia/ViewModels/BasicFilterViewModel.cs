using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Collectary.Search.Avalonia.ViewModels;

public partial class BasicFilterViewModel : ObservableObject
{
    private readonly ISearchUiCatalog _catalog;
    private readonly ILocalizationProvider _localization;
    private readonly Func<string, Task> _runQueryText;
    private readonly int _debounceMilliseconds;
    private readonly string _freeTextField;
    private readonly QueryOperatorKind _freeTextOperator;
    private readonly IReadOnlyList<string> _excludedChipFields;
    private readonly BasicQueryTranslator _translator = new(new QueryParser(new QueryLexer()), new QueryTextWriter());
    private readonly List<string> _chipLabels = new();
    private SearchUiSnapshot _snapshot = new();
    private bool _suppressRun;
    private CancellationTokenSource? _pendingCancellation;

    public ObservableCollection<FilterChipViewModel> Chips { get; } = new();
    public ObservableCollection<string> AddableFields { get; } = new();
    public ObservableCollection<string> SortFieldOptions { get; } = new();

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FieldSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsMoreFlyoutOpen { get; set; }

    [ObservableProperty]
    public partial string? SelectedSortField { get; set; }

    [ObservableProperty]
    public partial bool SortDescending { get; set; }

    internal Task? PendingRun { get; private set; }

    public int ActiveFilterCount => Chips.Count(chip => chip.HasSelection);

    public bool IsSortActive => SelectedSortField is not null;

    public BasicFilterViewModel(
        ISearchUiCatalog catalog,
        ILocalizationProvider localization,
        Func<string, Task> runQueryText,
        int debounceMilliseconds = 300,
        string freeTextField = "name",
        QueryOperatorKind freeTextOperator = QueryOperatorKind.Contains,
        IReadOnlyList<string>? excludedChipFields = null)
    {
        _catalog = catalog;
        _localization = localization;
        _runQueryText = runQueryText;
        _debounceMilliseconds = debounceMilliseconds;
        _freeTextField = freeTextField;
        _freeTextOperator = freeTextOperator;
        _excludedChipFields = excludedChipFields ?? [];
        Chips.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ActiveFilterCount));
    }

    public async Task LoadAsync()
    {
        _suppressRun = true;
        _snapshot = await _catalog.GetSnapshotAsync();
        _chipLabels.Clear();
        foreach (var field in _snapshot.Fields)
            if (!Same(field.Label, _freeTextField) && !_excludedChipFields.Any(e => Same(e, field.Label)))
                _chipLabels.Add(field.Label);
        SortFieldOptions.Clear();
        SortFieldOptions.Add(_freeTextField);
        foreach (var label in _chipLabels)
            SortFieldOptions.Add(label);
        RefreshAddableFields();
        _suppressRun = false;
    }

    public string ToQueryText()
    {
        var rows = new List<BasicConditionRow>();
        if (!string.IsNullOrWhiteSpace(SearchText))
            rows.Add(new BasicConditionRow(_freeTextField, _freeTextOperator, [SearchText.Trim()]));
        rows.AddRange(Chips.Select(c => c.ToRow()).Where(r => r is not null)!);
        return _translator.ToText(new BasicQueryModel
        {
            Rows = rows,
            Sort = SelectedSortField is null ? null : new BasicSort(SelectedSortField, SortDescending),
        });
    }

    public bool TryLoadFromText(string text)
    {
        var model = _translator.TryFromText(text);
        if (model is null)
            return false;

        string? searchText = null;
        var chips = new List<FilterChipViewModel>();
        foreach (var row in model.Rows)
        {
            if (Same(row.Field, _freeTextField))
            {
                if (row.Operator != _freeTextOperator || row.Values.Count != 1 || searchText is not null)
                    return false;
                searchText = row.Values[0];
                continue;
            }
            var chip = TryBuildChip(row);
            if (chip is null)
                return false;
            chips.Add(chip);
        }

        string? sortField = null;
        if (model.Sort is not null)
        {
            sortField = SortFieldOptions.FirstOrDefault(o => Same(o, model.Sort.Field));
            if (sortField is null)
                return false;
        }

        _suppressRun = true;
        SearchText = searchText ?? string.Empty;
        Chips.Clear();
        foreach (var chip in chips)
            Chips.Add(chip);
        SelectedSortField = sortField;
        SortDescending = model.Sort?.Descending ?? false;
        RefreshAddableFields();
        _suppressRun = false;
        return true;
    }

    [RelayCommand]
    private void AddChip(string label)
    {
        var canonical = _chipLabels.FirstOrDefault(l => Same(l, label));
        if (canonical is null)
            return;
        var chip = BuildChip(canonical);
        Chips.Add(chip);
        FieldSearchText = string.Empty;
        RefreshAddableFields();
        IsMoreFlyoutOpen = false;
        chip.IsFlyoutOpen = true;
    }

    [RelayCommand]
    private void RemoveChip(FilterChipViewModel chip)
    {
        Chips.Remove(chip);
        RefreshAddableFields();
        ScheduleRun();
    }

    partial void OnSearchTextChanged(string value) => ScheduleRun();

    partial void OnSelectedSortFieldChanged(string? value)
    {
        OnPropertyChanged(nameof(IsSortActive));
        ScheduleRun();
    }

    partial void OnSortDescendingChanged(bool value) => ScheduleRun();

    partial void OnFieldSearchTextChanged(string value) => RefreshAddableFields();

    private FilterChipViewModel? TryBuildChip(BasicConditionRow row)
    {
        var canonical = _chipLabels.FirstOrDefault(l => _snapshot.Find(l)?.MatchesLabel(row.Field) ?? Same(l, row.Field));
        if (canonical is null)
            return null;

        var chip = BuildChip(canonical);
        if (chip.IsChoiceStyle)
        {
            if (row.Operator is not (QueryOperatorKind.Equals or QueryOperatorKind.In))
                return null;
        }
        else if (row.Operator != chip.TextOperator || row.Values.Count != 1)
        {
            return null;
        }
        chip.ApplyValues(row.Values);
        return chip;
    }

    private FilterChipViewModel BuildChip(string label)
    {
        FilterChipViewModel chip = null!;
        chip = new FilterChipViewModel(
            label, SuggestionsFor(label), TextOperatorFor(label), _localization, OnChipValueChanged, () => RemoveChip(chip));
        return chip;
    }

    private void OnChipValueChanged()
    {
        OnPropertyChanged(nameof(ActiveFilterCount));
        ScheduleRun();
    }

    private IReadOnlyList<string> SuggestionsFor(string label) =>
        _snapshot.Find(label)?.ValueSuggestions ?? [];

    private QueryOperatorKind TextOperatorFor(string label) =>
        _snapshot.Find(label)?.Operators.Contains(QueryOperatorKind.Contains) == true
            ? QueryOperatorKind.Contains
            : QueryOperatorKind.Equals;

    private void RefreshAddableFields()
    {
        var available = _chipLabels.Where(label =>
            Chips.All(c => !Same(c.Label, label))
            && label.Contains(FieldSearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        AddableFields.Clear();
        foreach (var label in available)
            AddableFields.Add(label);
    }

    public void CancelPendingRun() => _pendingCancellation?.Cancel();

    private void ScheduleRun()
    {
        if (_suppressRun) return;
        _pendingCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        _pendingCancellation = cancellation;
        PendingRun = RunAfterDelayAsync(cancellation.Token);
    }

    private async Task RunAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(_debounceMilliseconds, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }
        await _runQueryText(ToQueryText());
    }

    private bool Same(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
