using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.Search;

public enum QuerySuggestionKind
{
    Field,
    Operator,
    Value,
    Keyword,
}

public sealed record QuerySuggestion(
    string Text,
    string InsertText,
    int ReplaceStart,
    int ReplaceLength,
    QuerySuggestionKind Kind);

public class QuerySuggestionEngine
{
    private readonly QueryLexer _lexer;
    private readonly PseudoFieldCatalog _pseudo;
    private readonly AsciiCaseFolding _folding = new();
    private readonly QueryTextWriter _writer = new();

    public QuerySuggestionEngine(QueryLexer lexer, PseudoFieldCatalog pseudo)
    {
        _lexer = lexer;
        _pseudo = pseudo;
    }

    public IReadOnlyList<QuerySuggestion> Suggest(string text, int caret, SearchCatalogSnapshot snapshot)
    {
        var head = text[..Math.Clamp(caret, 0, text.Length)];
        var lexed = _lexer.Tokenize(head);
        var tokens = lexed.Tokens.Where(t => t.Kind != QueryTokenKind.End).ToList();

        var prefix = "";
        var replaceStart = head.Length;
        var replaceLength = 0;
        var unterminated = lexed.Errors.FirstOrDefault(e => e.Code == QueryErrorCode.UnterminatedString);
        if (unterminated is not null)
        {
            prefix = head[(unterminated.Start + 1)..];
            replaceStart = unterminated.Start;
            replaceLength = head.Length - unterminated.Start;
        }
        else if (tokens.Count > 0
            && tokens[^1].Kind == QueryTokenKind.Word
            && tokens[^1].Start + tokens[^1].Length == head.Length)
        {
            var typing = tokens[^1];
            prefix = typing.Text;
            replaceStart = typing.Start;
            replaceLength = typing.Length;
            tokens.RemoveAt(tokens.Count - 1);
        }

        var (context, fieldLabel) = Classify(tokens);
        var candidates = Candidates(context, fieldLabel, snapshot);
        var folded = _folding.Fold(prefix);
        return candidates
            .Where(c => folded.Length == 0 || _folding.Fold(c.Text).Contains(folded, StringComparison.Ordinal))
            .OrderBy(c => _folding.Fold(c.Text).StartsWith(folded, StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(c => c.Text, StringComparer.OrdinalIgnoreCase)
            .Select(c => new QuerySuggestion(c.Text, c.InsertText, replaceStart, replaceLength, c.Kind))
            .ToList();
    }

    private enum Context
    {
        Field,
        Operator,
        NotContinuation,
        IsContinuation,
        IsNotContinuation,
        Value,
        InListOpen,
        InListValue,
        InListAfterValue,
        Connective,
        OrderField,
        OrderDirection,
        OrderAfterDirection,
    }

    private sealed record Candidate(string Text, string InsertText, QuerySuggestionKind Kind);

    private (Context Context, string? FieldLabel) Classify(List<QueryToken> tokens)
    {
        var context = Context.Field;
        string? field = null;
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            switch (context)
            {
                case Context.Field:
                    if (IsWord(token, "not") || token.Kind == QueryTokenKind.OpenParen) break;
                    if (AtOrderBy(tokens, i))
                    {
                        i++;
                        context = Context.OrderField;
                        break;
                    }
                    if (token.Kind is QueryTokenKind.Word or QueryTokenKind.String)
                    {
                        field = token.Text;
                        context = Context.Operator;
                    }
                    break;
                case Context.Operator:
                    if (IsComparison(token.Kind)) context = Context.Value;
                    else if (IsWord(token, "in")) context = Context.InListOpen;
                    else if (IsWord(token, "is")) context = Context.IsContinuation;
                    else if (IsWord(token, "not")) context = Context.NotContinuation;
                    break;
                case Context.NotContinuation:
                    context = IsWord(token, "in") ? Context.InListOpen : Context.Operator;
                    break;
                case Context.IsContinuation:
                    if (IsWord(token, "not")) context = Context.IsNotContinuation;
                    else if (IsWord(token, "empty")) context = Context.Connective;
                    break;
                case Context.IsNotContinuation:
                    if (IsWord(token, "empty")) context = Context.Connective;
                    break;
                case Context.Value:
                    if (token.Kind is QueryTokenKind.Word or QueryTokenKind.String) context = Context.Connective;
                    break;
                case Context.InListOpen:
                    if (token.Kind == QueryTokenKind.OpenParen) context = Context.InListValue;
                    break;
                case Context.InListValue:
                    if (token.Kind is QueryTokenKind.Word or QueryTokenKind.String) context = Context.InListAfterValue;
                    break;
                case Context.InListAfterValue:
                    if (token.Kind == QueryTokenKind.Comma) context = Context.InListValue;
                    else if (token.Kind == QueryTokenKind.CloseParen) context = Context.Connective;
                    break;
                case Context.Connective:
                    if (IsWord(token, "and") || IsWord(token, "or")) context = Context.Field;
                    else if (AtOrderBy(tokens, i))
                    {
                        i++;
                        context = Context.OrderField;
                    }
                    break;
                case Context.OrderField:
                    if (token.Kind is QueryTokenKind.Word or QueryTokenKind.String) context = Context.OrderDirection;
                    break;
                case Context.OrderDirection:
                    if (token.Kind == QueryTokenKind.Comma) context = Context.OrderField;
                    else if (IsWord(token, "asc") || IsWord(token, "desc")) context = Context.OrderAfterDirection;
                    break;
                case Context.OrderAfterDirection:
                    if (token.Kind == QueryTokenKind.Comma) context = Context.OrderField;
                    break;
            }
        }
        return (context, field);
    }

    private IReadOnlyList<Candidate> Candidates(Context context, string? fieldLabel, SearchCatalogSnapshot snapshot)
    {
        switch (context)
        {
            case Context.Field:
                return FieldCandidates(snapshot)
                    .Append(Keyword("NOT"))
                    .ToList();
            case Context.OrderField:
                return FieldCandidates(snapshot).ToList();
            case Context.Operator:
                return OperatorCandidates(fieldLabel, snapshot).ToList();
            case Context.NotContinuation:
                return [Keyword("in")];
            case Context.IsContinuation:
                return [Keyword("empty"), Keyword("not empty")];
            case Context.IsNotContinuation:
                return [Keyword("empty")];
            case Context.Value:
            case Context.InListValue:
                return ValueCandidates(fieldLabel, snapshot).ToList();
            case Context.InListOpen:
                return [Keyword("(")];
            case Context.Connective:
                return [Keyword("AND"), Keyword("OR"), Keyword("ORDER BY")];
            case Context.OrderDirection:
                return [Keyword("ASC"), Keyword("DESC")];
            default:
                return [];
        }
    }

    private IEnumerable<Candidate> FieldCandidates(SearchCatalogSnapshot snapshot)
    {
        var searchableLabels = snapshot.Fields
            .Where(g => g.Definitions.OfType<ISearchableFieldDefinition>().Any())
            .Select(g => g.Label);
        foreach (var label in searchableLabels.Concat(_pseudo.Labels))
            yield return new Candidate(label, _writer.WriteValue(label), QuerySuggestionKind.Field);
    }

    private IEnumerable<Candidate> OperatorCandidates(string? fieldLabel, SearchCatalogSnapshot snapshot)
    {
        if (fieldLabel is null) yield break;
        var operators = new HashSet<QueryOperatorKind>(_pseudo.OperatorsFor(fieldLabel));
        foreach (var definition in snapshot.FindField(fieldLabel)?.Definitions ?? [])
        {
            if (definition is ISearchableFieldDefinition searchable)
                operators.UnionWith(searchable.SupportedOperators);
        }
        foreach (var op in operators)
            yield return new Candidate(Display(op), Display(op), QuerySuggestionKind.Operator);
    }

    private IEnumerable<Candidate> ValueCandidates(string? fieldLabel, SearchCatalogSnapshot snapshot)
    {
        if (fieldLabel is null) yield break;
        if (Matches(fieldLabel, "preset") || Matches(fieldLabel, "collection"))
        {
            foreach (var preset in snapshot.Presets)
                yield return new Candidate(preset.Name, _writer.WriteValue(preset.Name), QuerySuggestionKind.Value);
            yield break;
        }
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in snapshot.FindField(fieldLabel)?.Definitions ?? [])
        {
            if (definition is not ISearchableFieldDefinition searchable) continue;
            foreach (var value in searchable.ValueSuggestions())
            {
                if (seen.Add(value))
                    yield return new Candidate(value, _writer.WriteValue(value), QuerySuggestionKind.Value);
            }
        }
    }

    private Candidate Keyword(string keyword) => new(keyword, keyword, QuerySuggestionKind.Keyword);

    private string Display(QueryOperatorKind op) => op switch
    {
        QueryOperatorKind.Equals => "=",
        QueryOperatorKind.NotEquals => "!=",
        QueryOperatorKind.Less => "<",
        QueryOperatorKind.LessOrEqual => "<=",
        QueryOperatorKind.Greater => ">",
        QueryOperatorKind.GreaterOrEqual => ">=",
        QueryOperatorKind.Contains => "~",
        QueryOperatorKind.NotContains => "!~",
        QueryOperatorKind.In => "in",
        QueryOperatorKind.IsEmpty => "is empty",
        _ => "is not empty",
    };

    private bool IsWord(QueryToken token, string keyword) =>
        token.Kind == QueryTokenKind.Word
        && string.Equals(token.Text, keyword, StringComparison.OrdinalIgnoreCase);

    private bool AtOrderBy(List<QueryToken> tokens, int index) =>
        IsWord(tokens[index], "order")
        && index + 1 < tokens.Count
        && IsWord(tokens[index + 1], "by");

    private bool IsComparison(QueryTokenKind kind) => kind is QueryTokenKind.Equals
        or QueryTokenKind.NotEquals or QueryTokenKind.Less or QueryTokenKind.LessOrEqual
        or QueryTokenKind.Greater or QueryTokenKind.GreaterOrEqual
        or QueryTokenKind.Contains or QueryTokenKind.NotContains;

    private bool Matches(string label, string pseudo) =>
        string.Equals(label, pseudo, StringComparison.OrdinalIgnoreCase);
}
