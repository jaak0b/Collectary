namespace Collectary.Search;

public sealed class BoundQuery<TItem>
{
    public BoundNode<TItem>? Root { get; init; }
    public IReadOnlyList<BoundOrderBy<TItem>> OrderBy { get; init; } = [];
}

public abstract class BoundNode<TItem>;

public sealed class BoundAndNode<TItem> : BoundNode<TItem>
{
    public BoundNode<TItem> Left { get; }
    public BoundNode<TItem> Right { get; }

    public BoundAndNode(BoundNode<TItem> left, BoundNode<TItem> right)
    {
        Left = left;
        Right = right;
    }
}

public sealed class BoundOrNode<TItem> : BoundNode<TItem>
{
    public BoundNode<TItem> Left { get; }
    public BoundNode<TItem> Right { get; }

    public BoundOrNode(BoundNode<TItem> left, BoundNode<TItem> right)
    {
        Left = left;
        Right = right;
    }
}

public sealed class BoundNotNode<TItem> : BoundNode<TItem>
{
    public BoundNode<TItem> Operand { get; }

    public BoundNotNode(BoundNode<TItem> operand) => Operand = operand;
}

public sealed class BoundConditionNode<TItem> : BoundNode<TItem>
{
    public required QueryOperatorKind Operator { get; init; }
    public required IReadOnlyList<BoundFieldMatch<TItem>> Bindings { get; init; }
}

public sealed record BoundFieldMatch<TItem>(
    IFieldConditionMatcher<TItem> Matcher, IReadOnlyList<Guid> DefinitionIds);

public sealed record BoundOrderBy<TItem>(Func<TItem, IComparable?> SortKey, bool Descending);

public sealed record QueryBindResult<TItem>(
    BoundQuery<TItem>? Query,
    IReadOnlyList<QueryError> Errors,
    IReadOnlyList<QueryNotice> Notices);
