using System.Linq.Expressions;

namespace Collectary.Search;

public class TypedValueMatcher<TItem, TValueBase, TValue> : IFieldConditionMatcher<TItem>
    where TValueBase : class
    where TValue : class, TValueBase
{
    private readonly ItemValueModel<TItem, TValueBase> _model;
    private readonly Func<TValue, bool> _predicate;
    private readonly Expression<Func<TValue, bool>>? _serverPredicate;

    public TypedValueMatcher(
        ItemValueModel<TItem, TValueBase> model,
        Func<TValue, bool> predicate,
        Expression<Func<TValue, bool>>? serverPredicate)
    {
        _model = model;
        _predicate = predicate;
        _serverPredicate = serverPredicate;
    }

    public Expression<Func<TItem, bool>>? ServerFilter(IReadOnlyCollection<Guid> definitionIds) =>
        _serverPredicate is null ? null : AnyValue(definitionIds, _serverPredicate);

    public bool Matches(TItem item, IReadOnlyCollection<Guid> definitionIds) =>
        _model.Values(item).OfType<TValue>()
            .Any(v => definitionIds.Contains(_model.DefinitionId(v)) && _predicate(v));

    private Expression<Func<TItem, bool>> AnyValue(
        IReadOnlyCollection<Guid> definitionIds, Expression<Func<TValue, bool>> valuePredicate)
    {
        var idList = definitionIds.ToList();
        var value = valuePredicate.Parameters[0];
        var definitionId = new ParameterReplacer(
                _model.DefinitionIdExpression.Parameters[0], value)
            .Visit(_model.DefinitionIdExpression.Body);
        var idMatches = Expression.Call(
            Expression.Constant(idList),
            typeof(List<Guid>).GetMethod(nameof(List<Guid>.Contains))!,
            definitionId);
        var inner = Expression.Lambda<Func<TValue, bool>>(
            Expression.AndAlso(idMatches, valuePredicate.Body), value);

        var item = _model.ValuesExpression.Parameters[0];
        var values = _model.ValuesExpression.Body;
        if (values is UnaryExpression { NodeType: ExpressionType.Convert } widened)
            values = widened.Operand;
        var ofType = Expression.Call(
            typeof(Enumerable), nameof(Enumerable.OfType), [typeof(TValue)], values);
        var any = Expression.Call(
            typeof(Enumerable), nameof(Enumerable.Any), [typeof(TValue)], ofType, inner);
        return Expression.Lambda<Func<TItem, bool>>(any, item);
    }
}

public class ValueEmptinessMatcher<TItem, TValueBase> : IFieldConditionMatcher<TItem>
    where TValueBase : class
{
    private readonly ItemValueModel<TItem, TValueBase> _model;
    private readonly bool _expectPresent;

    public ValueEmptinessMatcher(ItemValueModel<TItem, TValueBase> model, bool expectPresent)
    {
        _model = model;
        _expectPresent = expectPresent;
    }

    public Expression<Func<TItem, bool>>? ServerFilter(IReadOnlyCollection<Guid> definitionIds) => null;

    public bool Matches(TItem item, IReadOnlyCollection<Guid> definitionIds)
    {
        var hasNonEmpty = _model.Values(item)
            .Any(v => definitionIds.Contains(_model.DefinitionId(v)) && !_model.IsEmpty(v));
        return hasNonEmpty == _expectPresent;
    }
}

internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _from;
    private readonly Expression _to;

    public ParameterReplacer(ParameterExpression from, Expression to)
    {
        _from = from;
        _to = to;
    }

    protected override Expression VisitParameter(ParameterExpression node) =>
        node == _from ? _to : base.VisitParameter(node);
}
