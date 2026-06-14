using System.Linq.Expressions;

namespace Collectary.Search;

public class ServerFilterBuilder<TItem>
{
    public Expression<Func<TItem, bool>>? Build(BoundNode<TItem>? root) =>
        root is null ? null : Visit(root, wantUpperBound: true).Expr;

    private sealed record BoundFilter(Expression<Func<TItem, bool>>? Expr, bool? Constant);

    private BoundFilter Visit(BoundNode<TItem> node, bool wantUpperBound) => node switch
    {
        BoundAndNode<TItem> and => CombineAnd(Visit(and.Left, wantUpperBound), Visit(and.Right, wantUpperBound)),
        BoundOrNode<TItem> or => CombineOr(Visit(or.Left, wantUpperBound), Visit(or.Right, wantUpperBound)),
        BoundNotNode<TItem> not => Negate(Visit(not.Operand, !wantUpperBound)),
        BoundConditionNode<TItem> condition => VisitCondition(condition, wantUpperBound),
        _ => throw new InvalidOperationException($"Unknown bound node {node.GetType().Name}."),
    };

    private BoundFilter VisitCondition(BoundConditionNode<TItem> condition, bool wantUpperBound)
    {
        var filters = new List<Expression<Func<TItem, bool>>>();
        foreach (var binding in condition.Bindings)
        {
            var filter = binding.Matcher.ServerFilter(binding.DefinitionIds);
            if (filter is null) return new BoundFilter(null, wantUpperBound);
            filters.Add(filter);
        }
        if (condition.Operator == QueryOperatorKind.IsEmpty && filters.Count > 1)
            return new BoundFilter(null, wantUpperBound);
        return new BoundFilter(filters.Aggregate(Combine(Expression.OrElse)), null);
    }

    private BoundFilter CombineAnd(BoundFilter left, BoundFilter right)
    {
        if (left.Constant == false || right.Constant == false) return new BoundFilter(null, false);
        if (left.Constant == true) return right;
        if (right.Constant == true) return left;
        return new BoundFilter(Combine(Expression.AndAlso)(left.Expr!, right.Expr!), null);
    }

    private BoundFilter CombineOr(BoundFilter left, BoundFilter right)
    {
        if (left.Constant == true || right.Constant == true) return new BoundFilter(null, true);
        if (left.Constant == false) return right;
        if (right.Constant == false) return left;
        return new BoundFilter(Combine(Expression.OrElse)(left.Expr!, right.Expr!), null);
    }

    private BoundFilter Negate(BoundFilter inner)
    {
        if (inner.Constant is { } constant) return new BoundFilter(null, !constant);
        var parameter = inner.Expr!.Parameters[0];
        return new BoundFilter(
            Expression.Lambda<Func<TItem, bool>>(Expression.Not(inner.Expr.Body), parameter), null);
    }

    private Func<Expression<Func<TItem, bool>>, Expression<Func<TItem, bool>>, Expression<Func<TItem, bool>>>
        Combine(Func<Expression, Expression, BinaryExpression> merge) =>
        (left, right) =>
        {
            var parameter = left.Parameters[0];
            var rebound = new ParameterRebinder(right.Parameters[0], parameter).Visit(right.Body);
            return Expression.Lambda<Func<TItem, bool>>(merge(left.Body, rebound), parameter);
        };

    private sealed class ParameterRebinder : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterRebinder(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            node == _from ? _to : base.VisitParameter(node);
    }
}
