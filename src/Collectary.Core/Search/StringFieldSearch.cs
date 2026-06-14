using System.Linq.Expressions;
using Collectary.Core.Domain;

namespace Collectary.Core.Search;

public sealed class StringFieldSearch<TValue> : StringFieldSearch<Item, FieldValue, TValue>
    where TValue : FieldValue
{
    public StringFieldSearch(Func<TValue, string?> getter, Expression<Func<TValue, string?>> column)
        : base(new ItemSearchModel(), getter, column)
    {
    }
}
