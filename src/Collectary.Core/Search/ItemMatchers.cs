using System.Linq.Expressions;
using Collectary.Core.Domain;

namespace Collectary.Core.Search;

public sealed class TypedValueMatcher<TValue> : TypedValueMatcher<Item, FieldValue, TValue>
    where TValue : FieldValue
{
    public TypedValueMatcher(Func<TValue, bool> predicate, Expression<Func<TValue, bool>>? serverPredicate)
        : base(new ItemSearchModel(), predicate, serverPredicate)
    {
    }
}

public sealed class ValueEmptinessMatcher : ValueEmptinessMatcher<Item, FieldValue>
{
    public ValueEmptinessMatcher(bool expectPresent) : base(new ItemSearchModel(), expectPresent)
    {
    }
}
