namespace Collectary.Core.Search;

public enum QueryTokenKind
{
    Word,
    String,
    Equals,
    NotEquals,
    Less,
    LessOrEqual,
    Greater,
    GreaterOrEqual,
    Contains,
    NotContains,
    OpenParen,
    CloseParen,
    Comma,
    End,
}

public sealed record QueryToken(QueryTokenKind Kind, string Text, int Start, int Length);

public sealed record QueryLexResult(IReadOnlyList<QueryToken> Tokens, IReadOnlyList<QueryError> Errors);
