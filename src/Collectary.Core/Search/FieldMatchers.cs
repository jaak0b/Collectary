using System.Linq.Expressions;
using Collectary.Core.Domain;

namespace Collectary.Core.Search;

public class FieldServerFilters
{
    public Expression<Func<Item, bool>> AnyValue<TValue>(
        IReadOnlyCollection<Guid> definitionIds, Expression<Func<TValue, bool>> valuePredicate)
        where TValue : FieldValue
    {
        var value = valuePredicate.Parameters[0];
        var idList = definitionIds.ToList();
        var idMatches = Expression.Call(
            Expression.Constant(idList),
            typeof(List<Guid>).GetMethod(nameof(List<Guid>.Contains))!,
            Expression.Property(value, nameof(FieldValue.FieldDefinitionId)));
        var inner = Expression.Lambda<Func<TValue, bool>>(
            Expression.AndAlso(idMatches, valuePredicate.Body), value);

        var item = Expression.Parameter(typeof(Item), "item");
        var ofType = Expression.Call(
            typeof(Enumerable), nameof(Enumerable.OfType), [typeof(TValue)],
            Expression.Property(item, nameof(Item.Values)));
        var any = Expression.Call(
            typeof(Enumerable), nameof(Enumerable.Any), [typeof(TValue)], ofType, inner);
        return Expression.Lambda<Func<Item, bool>>(any, item);
    }
}

public sealed class TypedValueMatcher<TValue> : IFieldConditionMatcher
    where TValue : FieldValue
{
    private readonly Func<TValue, bool> _predicate;
    private readonly Expression<Func<TValue, bool>>? _serverPredicate;

    public TypedValueMatcher(Func<TValue, bool> predicate, Expression<Func<TValue, bool>>? serverPredicate)
    {
        _predicate = predicate;
        _serverPredicate = serverPredicate;
    }

    public Expression<Func<Item, bool>>? ServerFilter(IReadOnlyCollection<Guid> definitionIds) =>
        _serverPredicate is null ? null : new FieldServerFilters().AnyValue(definitionIds, _serverPredicate);

    public bool Matches(Item item, IReadOnlyCollection<Guid> definitionIds) =>
        item.Values.OfType<TValue>().Any(v => definitionIds.Contains(v.FieldDefinitionId) && _predicate(v));
}

public sealed class ValueEmptinessMatcher : IFieldConditionMatcher
{
    private readonly bool _expectPresent;

    public ValueEmptinessMatcher(bool expectPresent) => _expectPresent = expectPresent;

    public Expression<Func<Item, bool>>? ServerFilter(IReadOnlyCollection<Guid> definitionIds) => null;

    public bool Matches(Item item, IReadOnlyCollection<Guid> definitionIds)
    {
        var hasNonEmpty = item.Values.Any(v => definitionIds.Contains(v.FieldDefinitionId) && !v.IsEmpty);
        return hasNonEmpty == _expectPresent;
    }
}
