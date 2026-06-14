using System.Linq.Expressions;

namespace Collectary.Search;

public class StringFieldSearch<TItem, TValueBase, TValue>
    where TValueBase : class
    where TValue : class, TValueBase
{
    private readonly AsciiCaseFolding _folding = new();
    private readonly ItemValueModel<TItem, TValueBase> _model;
    private readonly Func<TValue, string?> _getter;
    private readonly Expression<Func<TValue, string?>> _column;

    public StringFieldSearch(
        ItemValueModel<TItem, TValueBase> model,
        Func<TValue, string?> getter,
        Expression<Func<TValue, string?>> column)
    {
        _model = model;
        _getter = getter;
        _column = column;
    }

    public IReadOnlyList<QueryOperatorKind> Operators =>
    [
        QueryOperatorKind.Equals, QueryOperatorKind.NotEquals,
        QueryOperatorKind.Contains, QueryOperatorKind.NotContains,
        QueryOperatorKind.In, QueryOperatorKind.IsEmpty, QueryOperatorKind.IsNotEmpty,
    ];

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher<TItem>? matcher, out QueryErrorCode? error)
    {
        matcher = null;
        error = null;
        switch (op)
        {
            case QueryOperatorKind.Equals:
            case QueryOperatorKind.NotEquals:
            {
                var folded = _folding.Fold(operands[0]);
                var expectEqual = op == QueryOperatorKind.Equals;
                matcher = new TypedValueMatcher<TItem, TValueBase, TValue>(
                    _model,
                    v => _getter(v) is { } text && _folding.AreEqual(text, folded) == expectEqual,
                    ServerEquality(folded, expectEqual));
                return true;
            }
            case QueryOperatorKind.Contains:
            case QueryOperatorKind.NotContains:
            {
                var folded = _folding.Fold(operands[0]);
                var expectContains = op == QueryOperatorKind.Contains;
                matcher = new TypedValueMatcher<TItem, TValueBase, TValue>(
                    _model,
                    v => _getter(v) is { } text && _folding.Contains(text, folded) == expectContains,
                    ServerContains(folded, expectContains));
                return true;
            }
            case QueryOperatorKind.In:
            {
                var folded = operands.Select(_folding.Fold).ToList();
                matcher = new TypedValueMatcher<TItem, TValueBase, TValue>(
                    _model,
                    v => _getter(v) is { } text && folded.Contains(_folding.Fold(text)),
                    ServerIn(folded));
                return true;
            }
            case QueryOperatorKind.IsEmpty:
                matcher = new ValueEmptinessMatcher<TItem, TValueBase>(_model, expectPresent: false);
                return true;
            case QueryOperatorKind.IsNotEmpty:
                matcher = new ValueEmptinessMatcher<TItem, TValueBase>(_model, expectPresent: true);
                return true;
            default:
                error = QueryErrorCode.OperatorNotSupported;
                return false;
        }
    }

    public IComparable? SortKey(TItem item, TValueBase? value) =>
        value is TValue typed ? _getter(typed) : null;

    private Expression<Func<TValue, bool>> ServerEquality(string folded, bool expectEqual)
    {
        var lowered = LoweredColumn();
        var operand = Expression.Constant(folded);
        var comparison = expectEqual
            ? Expression.Equal(lowered, operand)
            : (Expression)Expression.NotEqual(lowered, operand);
        return GuardedLambda(comparison);
    }

    private Expression<Func<TValue, bool>> ServerContains(string folded, bool expectContains)
    {
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        var call = Expression.Call(LoweredColumn(), containsMethod, Expression.Constant(folded));
        return GuardedLambda(expectContains ? call : Expression.Not(call));
    }

    private Expression<Func<TValue, bool>> ServerIn(List<string> folded)
    {
        var containsMethod = typeof(List<string>).GetMethod(nameof(List<string>.Contains))!;
        var call = Expression.Call(Expression.Constant(folded), containsMethod, LoweredColumn());
        return GuardedLambda(call);
    }

    private Expression LoweredColumn() =>
        Expression.Call(_column.Body, typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!);

    private Expression<Func<TValue, bool>> GuardedLambda(Expression comparison)
    {
        var notNull = Expression.NotEqual(_column.Body, Expression.Constant(null, typeof(string)));
        return Expression.Lambda<Func<TValue, bool>>(
            Expression.AndAlso(notNull, comparison), _column.Parameters[0]);
    }
}
