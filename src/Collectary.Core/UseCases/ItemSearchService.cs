using Collectary.Core.Ports;
using Collectary.Search;

namespace Collectary.Core.UseCases;

public class ItemSearchService : IItemSearchService
{
    private readonly IItemRepository _items;
    private readonly ISearchFieldCatalog _catalog;
    private readonly QueryParser _parser;
    private readonly QueryBinder _binder;
    private readonly ServerFilterBuilder _serverFilters;
    private readonly QueryEvaluator _evaluator;

    public ItemSearchService(
        IItemRepository items,
        ISearchFieldCatalog catalog,
        QueryParser parser,
        QueryBinder binder,
        ServerFilterBuilder serverFilters,
        QueryEvaluator evaluator)
    {
        _items = items;
        _catalog = catalog;
        _parser = parser;
        _binder = binder;
        _serverFilters = serverFilters;
        _evaluator = evaluator;
    }

    public async Task<ItemSearchResult> SearchAsync(string queryText)
    {
        var parsed = _parser.Parse(queryText);
        if (parsed.Query is null)
            return new ItemSearchResult([], parsed.Errors, []);

        var snapshot = await _catalog.GetSnapshotAsync();
        var bound = _binder.Bind(parsed.Query, snapshot);
        if (bound.Query is null)
            return new ItemSearchResult([], bound.Errors, bound.Notices);

        var serverFilter = _serverFilters.Build(bound.Query.Root);
        var candidates = await _items.SearchAsync(serverFilter);
        var matched = candidates.Where(item => _evaluator.Matches(bound.Query.Root, item));
        var sorted = _evaluator.Sort(matched, bound.Query.OrderBy);
        return new ItemSearchResult(sorted, bound.Errors, bound.Notices);
    }
}
