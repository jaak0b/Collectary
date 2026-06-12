using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Ports;
using Collectary.Core.Search;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class ItemQueryViewModel : ViewModelBase
{
    private const int MaxSuggestions = 12;

    private readonly IItemSearchService _searchService;
    private readonly ISearchFieldCatalog _catalog;
    private readonly QuerySuggestionEngine _suggestionEngine;
    private readonly Func<ItemSearchResult, Task> _onResults;
    private SearchCatalogSnapshot? _snapshot;

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
        IItemSearchService searchService,
        ISearchFieldCatalog catalog,
        QuerySuggestionEngine suggestionEngine,
        Func<ItemSearchResult, Task> onResults)
    {
        _searchService = searchService;
        _catalog = catalog;
        _suggestionEngine = suggestionEngine;
        _onResults = onResults;
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
        try
        {
            var result = await _searchService.SearchAsync(QueryText);
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
            AppLogger.Log.Error(ex, "Search failed for query {Query}", QueryText);
            QueryMessage = LocalizationService.Instance["SearchFailed"];
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
            AppLogger.Log.Error(ex, "Search suggestions failed for query {Query}", QueryText);
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

    private string Describe(QueryError error)
    {
        var loc = LocalizationService.Instance;
        return error.Code switch
        {
            QueryErrorCode.UnknownField => string.Format(loc["SearchUnknownField"], error.Detail),
            QueryErrorCode.FieldNotSearchable => string.Format(loc["SearchFieldNotSearchable"], error.Detail),
            QueryErrorCode.OperatorNotSupported => string.Format(loc["SearchOperatorNotSupported"], error.Detail),
            QueryErrorCode.InvalidValue => string.Format(loc["SearchInvalidValue"], error.Detail),
            _ => string.Format(loc["SearchSyntaxError"], error.Start + 1),
        };
    }

    private string DescribeNotice(QueryNotice notice) =>
        string.Format(LocalizationService.Instance["SearchNoticeSkipped"], notice.Field);
}
