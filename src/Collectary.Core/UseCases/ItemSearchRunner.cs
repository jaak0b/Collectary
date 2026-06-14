using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Search;

namespace Collectary.Core.UseCases;

public class ItemSearchRunner : ISearchRunner
{
    private readonly IItemSearchService _searchService;

    public ItemSearchRunner(IItemSearchService searchService) => _searchService = searchService;

    public async Task<SearchOutcome> SearchAsync(string queryText)
    {
        var result = await _searchService.SearchAsync(queryText);
        return new SearchOutcome(result.Items, result.Errors, result.Notices);
    }
}
