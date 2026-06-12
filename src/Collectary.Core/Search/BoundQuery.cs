using Collectary.Core.Domain;

namespace Collectary.Core.Search;

public sealed class BoundQuery
{
    public BoundNode? Root { get; init; }
    public IReadOnlyList<BoundOrderBy> OrderBy { get; init; } = [];
}

public abstract class BoundNode;

public sealed class BoundAndNode : BoundNode
{
    public BoundNode Left { get; }
    public BoundNode Right { get; }

    public BoundAndNode(BoundNode left, BoundNode right)
    {
        Left = left;
        Right = right;
    }
}

public sealed class BoundOrNode : BoundNode
{
    public BoundNode Left { get; }
    public BoundNode Right { get; }

    public BoundOrNode(BoundNode left, BoundNode right)
    {
        Left = left;
        Right = right;
    }
}

public sealed class BoundNotNode : BoundNode
{
    public BoundNode Operand { get; }

    public BoundNotNode(BoundNode operand) => Operand = operand;
}

public sealed class BoundConditionNode : BoundNode
{
    public required QueryOperatorKind Operator { get; init; }
    public required IReadOnlyList<BoundFieldMatch> Bindings { get; init; }
}

public sealed record BoundFieldMatch(IFieldConditionMatcher Matcher, IReadOnlyList<Guid> DefinitionIds);

public sealed record BoundOrderBy(Func<Item, IComparable?> SortKey, bool Descending);

public sealed record QueryNotice(QueryErrorCode Code, string Field);

public sealed record QueryBindResult(
    BoundQuery? Query,
    IReadOnlyList<QueryError> Errors,
    IReadOnlyList<QueryNotice> Notices);
