using System.Text;

namespace Collectary.Core.Search;

public class QueryTextWriter
{
    private readonly HashSet<string> _reservedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "or", "not", "in", "is", "empty", "order", "by", "asc", "desc",
    };

    public string Write(ParsedQuery query)
    {
        var parts = new List<string>();
        if (query.Root is not null)
            parts.Add(WriteNode(query.Root));
        if (query.OrderBy.Count > 0)
            parts.Add("ORDER BY " + string.Join(", ", query.OrderBy.Select(WriteOrderField)));
        return string.Join(" ", parts);
    }

    public string WriteValue(string raw) => NeedsQuoting(raw) ? Quote(raw) : raw;

    private string WriteNode(QueryNode node) => node switch
    {
        AndNode and => WriteAndOperand(and.Left) + " AND " + WriteAndOperand(and.Right),
        OrNode or => WriteNode(or.Left) + " OR " + WriteNode(or.Right),
        NotNode not => "NOT " + WriteNotOperand(not.Operand),
        ConditionNode condition => WriteCondition(condition),
        _ => "",
    };

    private string WriteAndOperand(QueryNode node) =>
        node is OrNode ? "(" + WriteNode(node) + ")" : WriteNode(node);

    private string WriteNotOperand(QueryNode node) =>
        node is ConditionNode ? WriteNode(node) : "(" + WriteNode(node) + ")";

    private string WriteCondition(ConditionNode condition)
    {
        var field = WriteValue(condition.Field);
        return condition.Operator switch
        {
            QueryOperatorKind.In =>
                field + " in (" + string.Join(", ", condition.Operands.Select(o => WriteValue(o.Text))) + ")",
            QueryOperatorKind.IsEmpty => field + " is empty",
            QueryOperatorKind.IsNotEmpty => field + " is not empty",
            _ => field + " " + SymbolFor(condition.Operator) + " "
                 + WriteValue(condition.Operands.Count > 0 ? condition.Operands[0].Text : ""),
        };
    }

    private string SymbolFor(QueryOperatorKind op) => op switch
    {
        QueryOperatorKind.Equals => "=",
        QueryOperatorKind.NotEquals => "!=",
        QueryOperatorKind.Less => "<",
        QueryOperatorKind.LessOrEqual => "<=",
        QueryOperatorKind.Greater => ">",
        QueryOperatorKind.GreaterOrEqual => ">=",
        QueryOperatorKind.Contains => "~",
        _ => "!~",
    };

    private string WriteOrderField(OrderByField field) =>
        WriteValue(field.Field) + (field.Descending ? " DESC" : "");

    private bool NeedsQuoting(string raw) =>
        raw.Length == 0
        || _reservedWords.Contains(raw)
        || raw.Any(c => char.IsWhiteSpace(c) || c == '\\' || IsSpecial(c));

    private bool IsSpecial(char c) => c is '=' or '!' or '<' or '>' or '~' or '(' or ')' or ',' or '"';

    private string Quote(string raw)
    {
        var quoted = new StringBuilder("\"");
        foreach (var c in raw)
        {
            if (c is '"' or '\\')
                quoted.Append('\\');
            quoted.Append(c);
        }
        return quoted.Append('"').ToString();
    }
}
