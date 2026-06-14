using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Collectary.Search.ViewModels;

public partial class ItemQueryViewModel : ObservableObject
{
    private const int MaxSuggestions = 12;

    private readonly ISearchRunner _searchRunner;
    private readonly ISearchUiCatalog _catalog;
    private readonly QuerySuggestionEngine _suggestionEngine;
    private readonly ILocalizationProvider _localization;
    private readonly Func<SearchOutcome, Task> _onResults;
    private readonly Action<Exception, string>? _logError;
    private SearchUiSnapshot? _snapshot;
    private int _runSequence;

    [ObservableProperty]
    public partial string QueryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int CaretIndex { get; set; }

    [ObservableProperty]
    public partial bool AreSuggestionsOpen { get; set; }

    [ObservableProperty]
    public partial int SelectedSuggestionIndex { get; set; } = -1;

    [ObservableProperty]
    public partial string? QueryMessage { get; set; }

    public ObservableCollection<QuerySuggestion> Suggestions { get; } = new();

    public ItemQueryViewModel(
        ISearchRunner searchRunner,
        ISearchUiCatalog catalog,
        QuerySuggestionEngine suggestionEngine,
        ILocalizationProvider localization,
        Func<SearchOutcome, Task> onResults,
        Action<Exception, string>? logError = null)
    {
        _searchRunner = searchRunner;
        _catalog = catalog;
        _suggestionEngine = suggestionEngine;
        _localization = localization;
        _onResults = onResults;
        _logError = logError;
    }

    public QuerySuggestion? SelectedSuggestion =>
        SelectedSuggestionIndex >= 0 && SelectedSuggestionIndex < Suggestions.Count
            ? Suggestions[SelectedSuggestionIndex]
            : null;

    public void ResetSnapshot() => _snapshot = null;

    public void MoveSelection(int delta)
    {
        if (Suggestions.Count == 0) return;
        var count = Suggestions.Count;
        SelectedSuggestionIndex = ((SelectedSuggestionIndex + delta) % count + count) % count;
        AreSuggestionsOpen = true;
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        AreSuggestionsOpen = false;
        var sequence = ++_runSequence;
        try
        {
            var result = await _searchRunner.SearchAsync(QueryText);
            if (sequence != _runSequence)
                return;
            if (result.Errors.Count > 0)
            {
                QueryMessage = Describe(result.Errors[0]);
                return;
            }
            QueryMessage = result.Notices.Count > 0 ? DescribeNotice(result.Notices[0]) : null;
            await _onResults(result);
        }
        catch (Exception ex)
        {
            _logError?.Invoke(ex, $"Search failed for query {QueryText}");
            QueryMessage = _localization.Get(SearchLocalizationKeys.SearchFailed);
        }
    }

    [RelayCommand]
    private async Task RefreshSuggestionsAsync()
    {
        try
        {
            _snapshot ??= await _catalog.GetSnapshotAsync();
            var suggestions = _suggestionEngine.Suggest(QueryText, CaretIndex, _snapshot);
            Suggestions.Clear();
            foreach (var suggestion in suggestions.Take(MaxSuggestions))
                Suggestions.Add(suggestion);
            SelectedSuggestionIndex = Suggestions.Count > 0 ? 0 : -1;
            AreSuggestionsOpen = Suggestions.Count > 0;
        }
        catch (Exception ex)
        {
            _logError?.Invoke(ex, $"Search suggestions failed for query {QueryText}");
            AreSuggestionsOpen = false;
        }
    }

    [RelayCommand]
    private async Task AcceptSuggestionAsync(QuerySuggestion? suggestion)
    {
        suggestion ??= SelectedSuggestion;
        if (suggestion is null) return;
        var text = QueryText;
        var start = Math.Clamp(suggestion.ReplaceStart, 0, text.Length);
        var end = Math.Clamp(suggestion.ReplaceStart + suggestion.ReplaceLength, start, text.Length);
        QueryText = text[..start] + suggestion.InsertText + " " + text[end..];
        CaretIndex = start + suggestion.InsertText.Length + 1;
        await RefreshSuggestionsAsync();
    }

    [RelayCommand]
    private void CloseSuggestions() => AreSuggestionsOpen = false;

    private string Describe(QueryError error) => error.Code switch
    {
        QueryErrorCode.UnknownField => string.Format(_localization.Get(SearchLocalizationKeys.SearchUnknownField), error.Detail),
        QueryErrorCode.FieldNotSearchable => string.Format(_localization.Get(SearchLocalizationKeys.SearchFieldNotSearchable), error.Detail),
        QueryErrorCode.OperatorNotSupported => string.Format(_localization.Get(SearchLocalizationKeys.SearchOperatorNotSupported), error.Detail),
        QueryErrorCode.InvalidValue => string.Format(_localization.Get(SearchLocalizationKeys.SearchInvalidValue), error.Detail),
        _ => string.Format(_localization.Get(SearchLocalizationKeys.SearchSyntaxError), error.Start + 1),
    };

    private string DescribeNotice(QueryNotice notice) =>
        string.Format(_localization.Get(SearchLocalizationKeys.SearchNoticeSkipped), notice.Field);
}
