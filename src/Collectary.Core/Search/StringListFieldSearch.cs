using Collectary.Core.Domain;

namespace Collectary.Core.Search;

public sealed class StringListFieldSearch<TValue> : StringListFieldSearch<Item, FieldValue, TValue>
    where TValue : FieldValue
{
    public StringListFieldSearch(Func<TValue, IReadOnlyList<string>> getter)
        : base(new ItemSearchModel(), getter)
    {
    }
}
