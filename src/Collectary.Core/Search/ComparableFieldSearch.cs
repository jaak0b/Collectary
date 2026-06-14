using System.Linq.Expressions;
using Collectary.Core.Domain;

namespace Collectary.Core.Search;

public sealed class ComparableFieldSearch<TValue, TComparable>
    : ComparableFieldSearch<Item, FieldValue, TValue, TComparable>
    where TValue : FieldValue
    where TComparable : struct, IComparable<TComparable>
{
    public ComparableFieldSearch(
        Func<TValue, TComparable?> getter,
        Expression<Func<TValue, TComparable?>> column,
        Func<string, TComparable?> parser,
        bool ordered = true,
        Func<string, Func<TValue, bool>?>? operandConstraint = null)
        : base(new ItemSearchModel(), getter, column, parser, ordered, operandConstraint)
    {
    }
}
