using System.Linq.Expressions;
using Collectary.Core.Domain;

namespace Collectary.Core.Search;

public class ComparableFieldSearch<TValue, TComparable>
    where TValue : FieldValue
    where TComparable : struct, IComparable<TComparable>
{
    private readonly Func<TValue, TComparable?> _getter;
    private readonly Expression<Func<TValue, TComparable?>> _column;
    private readonly Func<string, TComparable?> _parser;
    private readonly bool _ordered;

    public ComparableFieldSearch(
        Func<TValue, TComparable?> getter,
        Expression<Func<TValue, TComparable?>> column,
        Func<string, TComparable?> parser,
        bool ordered = true)
    {
        _getter = getter;
        _column = column;
        _parser = parser;
        _ordered = ordered;
    }

    public IReadOnlyList<QueryOperatorKind> Operators
    {
        get
        {
            var operators = new List<QueryOperatorKind>
            {
                QueryOperatorKind.Equals, QueryOperatorKind.NotEquals,
                QueryOperatorKind.In, QueryOperatorKind.IsEmpty, QueryOperatorKind.IsNotEmpty,
            };
            if (_ordered)
                operators.AddRange(
                [
                    QueryOperatorKind.Less, QueryOperatorKind.LessOrEqual,
                    QueryOperatorKind.Greater, QueryOperatorKind.GreaterOrEqual,
                ]);
            return operators;
        }
    }

    public bool TryCreateMatcher(QueryOperatorKind op, IReadOnlyList<string> operands,
        out IFieldConditionMatcher? matcher, out QueryErrorCode? error)
    {
        matcher = null;
        error = null;
        if (op == QueryOperatorKind.IsEmpty)
        {
            matcher = new ValueEmptinessMatcher(expectPresent: false);
            return true;
        }
        if (op == QueryOperatorKind.IsNotEmpty)
        {
            matcher = new ValueEmptinessMatcher(expectPresent: true);
            return true;
        }
        if (!Operators.Contains(op))
        {
            error = QueryErrorCode.OperatorNotSupported;
            return false;
        }
        if (op == QueryOperatorKind.In)
        {
            var values = new List<TComparable>();
            foreach (var operand in operands)
            {
                if (_parser(operand) is not { } parsed)
                {
                    error = QueryErrorCode.InvalidValue;
                    return false;
                }
                values.Add(parsed);
            }
            matcher = new TypedValueMatcher<TValue>(
                v => _getter(v) is { } x && values.Any(val => x.CompareTo(val) == 0),
                ServerIn(values));
            return true;
        }
        if (_parser(operands[0]) is not { } value)
        {
            error = QueryErrorCode.InvalidValue;
            return false;
        }
        matcher = new TypedValueMatcher<TValue>(
            MemoryPredicate(op, value),
            ServerCompare(op, value));
        return true;
    }

    public IComparable? SortKey(Item item, FieldValue? value) =>
        value is TValue typed && _getter(typed) is { } x ? x as IComparable : null;

    private Func<TValue, bool> MemoryPredicate(QueryOperatorKind op, TComparable operand) => op switch
    {
        QueryOperatorKind.Equals => v => _getter(v) is { } x && x.CompareTo(operand) == 0,
        QueryOperatorKind.NotEquals => v => _getter(v) is { } x && x.CompareTo(operand) != 0,
        QueryOperatorKind.Less => v => _getter(v) is { } x && x.CompareTo(operand) < 0,
        QueryOperatorKind.LessOrEqual => v => _getter(v) is { } x && x.CompareTo(operand) <= 0,
        QueryOperatorKind.Greater => v => _getter(v) is { } x && x.CompareTo(operand) > 0,
        _ => v => _getter(v) is { } x && x.CompareTo(operand) >= 0,
    };

    private Expression<Func<TValue, bool>> ServerCompare(QueryOperatorKind op, TComparable operand)
    {
        var comparisonType = op switch
        {
            QueryOperatorKind.Equals => ExpressionType.Equal,
            QueryOperatorKind.NotEquals => ExpressionType.NotEqual,
            QueryOperatorKind.Less => ExpressionType.LessThan,
            QueryOperatorKind.LessOrEqual => ExpressionType.LessThanOrEqual,
            QueryOperatorKind.Greater => ExpressionType.GreaterThan,
            _ => ExpressionType.GreaterThanOrEqual,
        };
        var comparison = Expression.MakeBinary(
            comparisonType, _column.Body, Expression.Constant(operand, typeof(TComparable?)));
        return GuardedLambda(comparison);
    }

    private Expression<Func<TValue, bool>> ServerIn(List<TComparable> values)
    {
        var nullableValues = values.Select(v => (TComparable?)v).ToList();
        var containsMethod = typeof(List<TComparable?>).GetMethod(nameof(List<int>.Contains))!;
        var call = Expression.Call(Expression.Constant(nullableValues), containsMethod, _column.Body);
        return GuardedLambda(call);
    }

    private Expression<Func<TValue, bool>> GuardedLambda(Expression comparison)
    {
        var notNull = Expression.NotEqual(_column.Body, Expression.Constant(null, typeof(TComparable?)));
        return Expression.Lambda<Func<TValue, bool>>(
            Expression.AndAlso(notNull, comparison), _column.Parameters[0]);
    }
}
