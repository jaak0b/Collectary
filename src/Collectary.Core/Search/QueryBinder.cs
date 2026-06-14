using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.Search;

public sealed class QueryBinder
{
    private readonly PseudoFieldCatalog _pseudo;

    public QueryBinder(PseudoFieldCatalog pseudo) => _pseudo = pseudo;

    public QueryBindResult<Item> Bind(ParsedQuery query, SearchCatalogSnapshot snapshot) =>
        new QueryBinder<Item>(new ItemSearchCatalog(_pseudo, snapshot)).Bind(query);
}
