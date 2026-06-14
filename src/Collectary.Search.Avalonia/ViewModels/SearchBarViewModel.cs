using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Collectary.Search.Avalonia.ViewModels;

public partial class SearchBarViewModel : ObservableObject
{
    private readonly ILocalizationProvider _localization;
    private readonly Func<bool> _loadBasicModePreference;
    private readonly Action<bool> _saveBasicModePreference;

    public ItemQueryViewModel Query { get; }
    public BasicFilterViewModel BasicFilter { get; }

    [ObservableProperty]
    public partial bool IsBasicMode { get; set; }

    [ObservableProperty]
    public partial bool IsFilterPanelExpanded { get; set; }

    public int ActiveFilterCount => BasicFilter.ActiveFilterCount;

    public bool IsSortActive => BasicFilter.IsSortActive;

    public string FiltersLabel => ActiveFilterCount > 0
        ? string.Format(_localization.Get(SearchLocalizationKeys.SearchFiltersWithCount), ActiveFilterCount)
        : _localization.Get(SearchLocalizationKeys.SearchFilters);

    public SearchBarViewModel(
        ItemQueryViewModel query,
        BasicFilterViewModel basicFilter,
        ILocalizationProvider localization,
        Func<bool> loadBasicModePreference,
        Action<bool> saveBasicModePreference)
    {
        Query = query;
        BasicFilter = basicFilter;
        _localization = localization;
        _loadBasicModePreference = loadBasicModePreference;
        _saveBasicModePreference = saveBasicModePreference;
        BasicFilter.PropertyChanged += OnBasicFilterChanged;
    }

    private void OnBasicFilterChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BasicFilterViewModel.ActiveFilterCount))
        {
            OnPropertyChanged(nameof(ActiveFilterCount));
            OnPropertyChanged(nameof(FiltersLabel));
        }
        else if (e.PropertyName == nameof(BasicFilterViewModel.IsSortActive))
        {
            OnPropertyChanged(nameof(IsSortActive));
        }
    }

    public async Task InitializeAsync(string defaultQuery)
    {
        Query.ResetSnapshot();
        await BasicFilter.LoadAsync();
        IsBasicMode = _loadBasicModePreference() && BasicFilter.TryLoadFromText(defaultQuery);
        Query.QueryText = defaultQuery;
        await Query.RunCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void SwitchToAdvanced()
    {
        BasicFilter.CancelPendingRun();
        Query.QueryText = BasicFilter.ToQueryText();
        IsBasicMode = false;
        _saveBasicModePreference(false);
    }

    [RelayCommand]
    private void SwitchToBasic()
    {
        if (!BasicFilter.TryLoadFromText(Query.QueryText))
        {
            Query.QueryMessage = _localization.Get(SearchLocalizationKeys.SearchTooComplexForBasic);
            return;
        }
        IsBasicMode = true;
        Query.QueryMessage = null;
        _saveBasicModePreference(true);
    }

    public string SearchPlaceholder => _localization.Get(SearchLocalizationKeys.SearchPlaceholder);
    public string SearchLabel => _localization.Get(SearchLocalizationKeys.Search);
    public string SwitchToBasicLabel => _localization.Get(SearchLocalizationKeys.SearchSwitchToBasic);
    public string SwitchToAdvancedLabel => _localization.Get(SearchLocalizationKeys.SearchSwitchToAdvanced);
    public string ItemsPlaceholder => _localization.Get(SearchLocalizationKeys.SearchItemsPlaceholder);
    public string MoreLabel => _localization.Get(SearchLocalizationKeys.SearchMore);
    public string FindFieldsPlaceholder => _localization.Get(SearchLocalizationKeys.SearchFindFields);
    public string SortByLabel => _localization.Get(SearchLocalizationKeys.SearchSortBy);
    public string SortNoneLabel => _localization.Get(SearchLocalizationKeys.SearchSortNone);
    public string SortAscendingLabel => _localization.Get(SearchLocalizationKeys.SearchSortAscending);
    public string SortDescendingLabel => _localization.Get(SearchLocalizationKeys.SearchSortDescending);

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnPropertyChanged(nameof(SearchLabel));
        OnPropertyChanged(nameof(SwitchToBasicLabel));
        OnPropertyChanged(nameof(SwitchToAdvancedLabel));
        OnPropertyChanged(nameof(ItemsPlaceholder));
        OnPropertyChanged(nameof(MoreLabel));
        OnPropertyChanged(nameof(FindFieldsPlaceholder));
        OnPropertyChanged(nameof(SortByLabel));
        OnPropertyChanged(nameof(SortNoneLabel));
        OnPropertyChanged(nameof(SortAscendingLabel));
        OnPropertyChanged(nameof(SortDescendingLabel));
        OnPropertyChanged(nameof(FiltersLabel));
        foreach (var chip in BasicFilter.Chips)
            chip.RefreshLocalization();
    }
}
