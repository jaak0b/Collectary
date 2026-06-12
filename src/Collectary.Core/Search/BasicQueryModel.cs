namespace Collectary.Core.Search;

public sealed record BasicConditionRow(string Field, QueryOperatorKind Operator, IReadOnlyList<string> Values);

public sealed record BasicSort(string Field, bool Descending);

public sealed class BasicQueryModel
{
    public IReadOnlyList<BasicConditionRow> Rows { get; init; } = [];
    public BasicSort? Sort { get; init; }
}

public class BasicQueryTranslator
{
    private readonly QueryParser _parser;
    private readonly QueryTextWriter _writer;

    public BasicQueryTranslator(QueryParser parser, QueryTextWriter writer)
    {
        _parser = parser;
        _writer = writer;
    }

    public BasicQueryModel? TryFromText(string text)
    {
        var parsed = _parser.Parse(text);
        if (parsed.Errors.Count > 0 || parsed.Query is null)
            return null;
        if (parsed.Query.OrderBy.Count > 1)
            return null;

        var rows = new List<BasicConditionRow>();
        if (parsed.Query.Root is not null && !TryFlatten(parsed.Query.Root, rows))
            return null;
        if (HasDuplicateFields(rows))
            return null;

        var orderBy = parsed.Query.OrderBy.Count == 1 ? parsed.Query.OrderBy[0] : null;
        return new BasicQueryModel
        {
            Rows = rows,
            Sort = orderBy is null ? null : new BasicSort(orderBy.Field, orderBy.Descending),
        };
    }

    public string ToText(BasicQueryModel model)
    {
        QueryNode? root = null;
        foreach (var row in model.Rows)
        {
            var condition = new ConditionNode
            {
                Field = row.Field,
                FieldStart = 0,
                FieldLength = 0,
                Operator = row.Operator,
                Operands = row.Values.Select(v => new QueryOperand(v, false, 0, 0)).ToList(),
            };
            root = root is null ? condition : new AndNode(root, condition);
        }

        var query = new ParsedQuery
        {
            Root = root,
            OrderBy = model.Sort is null
                ? []
                : [new OrderByField(model.Sort.Field, model.Sort.Descending, 0, 0)],
        };
        return _writer.Write(query);
    }

    private bool TryFlatten(QueryNode node, List<BasicConditionRow> rows)
    {
        if (node is AndNode and)
            return TryFlatten(and.Left, rows) && TryFlatten(and.Right, rows);
        if (node is not ConditionNode condition)
            return false;
        if (condition.Operator is not (QueryOperatorKind.Equals or QueryOperatorKind.In or QueryOperatorKind.Contains))
            return false;
        if (condition.Operands.Any(o => o.Text.Contains(',')))
            return false;

        rows.Add(new BasicConditionRow(
            condition.Field, condition.Operator, condition.Operands.Select(o => o.Text).ToList()));
        return true;
    }

    private bool HasDuplicateFields(List<BasicConditionRow> rows) =>
        rows.Select(r => r.Field).Distinct(StringComparer.OrdinalIgnoreCase).Count() != rows.Count;
}
