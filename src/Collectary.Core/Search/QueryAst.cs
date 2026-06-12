namespace Collectary.Core.Search;

public abstract class QueryNode;

public sealed class AndNode : QueryNode
{
    public QueryNode Left { get; }
    public QueryNode Right { get; }

    public AndNode(QueryNode left, QueryNode right)
    {
        Left = left;
        Right = right;
    }
}

public sealed class OrNode : QueryNode
{
    public QueryNode Left { get; }
    public QueryNode Right { get; }

    public OrNode(QueryNode left, QueryNode right)
    {
        Left = left;
        Right = right;
    }
}

public sealed class NotNode : QueryNode
{
    public QueryNode Operand { get; }

    public NotNode(QueryNode operand) => Operand = operand;
}

public sealed record QueryOperand(string Text, bool WasQuoted, int Start, int Length);

public sealed class ConditionNode : QueryNode
{
    public required string Field { get; init; }
    public required int FieldStart { get; init; }
    public required int FieldLength { get; init; }
    public required QueryOperatorKind Operator { get; init; }
    public IReadOnlyList<QueryOperand> Operands { get; init; } = [];
}

public sealed record OrderByField(string Field, bool Descending, int Start, int Length);

public sealed class ParsedQuery
{
    public QueryNode? Root { get; init; }
    public IReadOnlyList<OrderByField> OrderBy { get; init; } = [];
}

public sealed record QueryParseResult(ParsedQuery? Query, IReadOnlyList<QueryError> Errors);
