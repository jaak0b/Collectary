using System.Globalization;
using Collectary.Core.Domain;

namespace Collectary.Core.Search;

public class QueryEvaluator
{
    public bool Matches(BoundNode? node, Item item) => node switch
    {
        null => true,
        BoundAndNode and => Matches(and.Left, item) && Matches(and.Right, item),
        BoundOrNode or => Matches(or.Left, item) || Matches(or.Right, item),
        BoundNotNode not => !Matches(not.Operand, item),
        BoundConditionNode condition => MatchesCondition(condition, item),
        _ => throw new InvalidOperationException($"Unknown bound node {node.GetType().Name}."),
    };

    public IReadOnlyList<Item> Sort(IEnumerable<Item> items, IReadOnlyList<BoundOrderBy> orderBy)
    {
        if (orderBy.Count == 0) return items.ToList();
        var comparer = new SortKeyComparer();
        IOrderedEnumerable<Item>? ordered = null;
        foreach (var key in orderBy)
        {
            ordered = ordered is null
                ? items.OrderBy(item => key.SortKey(item) is null ? 1 : 0)
                : ordered.ThenBy(item => key.SortKey(item) is null ? 1 : 0);
            ordered = key.Descending
                ? ordered.ThenByDescending(key.SortKey, comparer)
                : ordered.ThenBy(key.SortKey, comparer);
        }
        return ordered!.ToList();
    }

    private bool MatchesCondition(BoundConditionNode condition, Item item) =>
        condition.Operator == QueryOperatorKind.IsEmpty
            ? condition.Bindings.All(b => b.Matcher.Matches(item, b.DefinitionIds))
            : condition.Bindings.Any(b => b.Matcher.Matches(item, b.DefinitionIds));

    private sealed class SortKeyComparer : IComparer<IComparable?>
    {
        public int Compare(IComparable? x, IComparable? y)
        {
            if (x is null || y is null) return 0;
            if (IsNumeric(x) && IsNumeric(y))
                return Convert.ToDecimal(x, CultureInfo.InvariantCulture)
                    .CompareTo(Convert.ToDecimal(y, CultureInfo.InvariantCulture));
            if (x.GetType() == y.GetType())
            {
                if (x is string left && y is string right)
                    return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
                return x.CompareTo(y);
            }
            return string.Compare(
                Convert.ToString(x, CultureInfo.InvariantCulture),
                Convert.ToString(y, CultureInfo.InvariantCulture),
                StringComparison.OrdinalIgnoreCase);
        }

        private bool IsNumeric(IComparable value) =>
            value is byte or short or int or long or float or double or decimal;
    }
}
