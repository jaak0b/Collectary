using System.Reflection;

namespace Collectary.Search.Avalonia;

public sealed class SearchLocalizationKeys
{
    public const string Search = "Search";
    public const string SearchAllValues = "SearchAllValues";
    public const string SearchClear = "SearchClear";
    public const string SearchContainsLabel = "SearchContainsLabel";
    public const string SearchEqualsLabel = "SearchEqualsLabel";
    public const string SearchFailed = "SearchFailed";
    public const string SearchFieldNotSearchable = "SearchFieldNotSearchable";
    public const string SearchFilters = "SearchFilters";
    public const string SearchFiltersWithCount = "SearchFiltersWithCount";
    public const string SearchFindFields = "SearchFindFields";
    public const string SearchFindValues = "SearchFindValues";
    public const string SearchInvalidValue = "SearchInvalidValue";
    public const string SearchItemsPlaceholder = "SearchItemsPlaceholder";
    public const string SearchMore = "SearchMore";
    public const string SearchNoticeSkipped = "SearchNoticeSkipped";
    public const string SearchOperatorNotSupported = "SearchOperatorNotSupported";
    public const string SearchPlaceholder = "SearchPlaceholder";
    public const string SearchRemoveFilter = "SearchRemoveFilter";
    public const string SearchSelectedCount = "SearchSelectedCount";
    public const string SearchSortAscending = "SearchSortAscending";
    public const string SearchSortBy = "SearchSortBy";
    public const string SearchSortDescending = "SearchSortDescending";
    public const string SearchSortNone = "SearchSortNone";
    public const string SearchSwitchToAdvanced = "SearchSwitchToAdvanced";
    public const string SearchSwitchToBasic = "SearchSwitchToBasic";
    public const string SearchSyntaxError = "SearchSyntaxError";
    public const string SearchTooComplexForBasic = "SearchTooComplexForBasic";
    public const string SearchUnknownField = "SearchUnknownField";
    public const string SearchValuePlaceholder = "SearchValuePlaceholder";

    public IReadOnlyList<string> All { get; } = typeof(SearchLocalizationKeys)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral)
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToList();
}
