using Collectary.Core.Domain;
using Collectary.Search;

namespace Collectary.Core.Ports;

public interface IItemSearchService
{
    Task<ItemSearchResult> SearchAsync(string queryText);
}

public sealed record ItemSearchResult(
    IReadOnlyList<Item> Items,
    IReadOnlyList<QueryError> Errors,
    IReadOnlyList<QueryNotice> Notices);
