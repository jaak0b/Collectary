using System.Text;

namespace Collectary.Core.Search;

public class QueryLexer
{
    public QueryLexResult Tokenize(string text)
    {
        var tokens = new List<QueryToken>();
        var errors = new List<QueryError>();
        var pos = 0;
        while (pos < text.Length)
        {
            var c = text[pos];
            if (char.IsWhiteSpace(c))
            {
                pos++;
                continue;
            }
            switch (c)
            {
                case '(':
                    tokens.Add(new QueryToken(QueryTokenKind.OpenParen, "(", pos, 1));
                    pos++;
                    break;
                case ')':
                    tokens.Add(new QueryToken(QueryTokenKind.CloseParen, ")", pos, 1));
                    pos++;
                    break;
                case ',':
                    tokens.Add(new QueryToken(QueryTokenKind.Comma, ",", pos, 1));
                    pos++;
                    break;
                case '=':
                    tokens.Add(new QueryToken(QueryTokenKind.Equals, "=", pos, 1));
                    pos++;
                    break;
                case '~':
                    tokens.Add(new QueryToken(QueryTokenKind.Contains, "~", pos, 1));
                    pos++;
                    break;
                case '<':
                    pos = ReadComparator(text, pos, tokens, QueryTokenKind.Less, QueryTokenKind.LessOrEqual);
                    break;
                case '>':
                    pos = ReadComparator(text, pos, tokens, QueryTokenKind.Greater, QueryTokenKind.GreaterOrEqual);
                    break;
                case '!':
                    pos = ReadBang(text, pos, tokens, errors);
                    break;
                case '"':
                    pos = ReadString(text, pos, tokens, errors);
                    break;
                default:
                    pos = ReadWord(text, pos, tokens);
                    break;
            }
        }
        tokens.Add(new QueryToken(QueryTokenKind.End, "", text.Length, 0));
        return new QueryLexResult(tokens, errors);
    }

    private int ReadComparator(string text, int pos, List<QueryToken> tokens,
        QueryTokenKind bare, QueryTokenKind withEquals)
    {
        if (pos + 1 < text.Length && text[pos + 1] == '=')
        {
            tokens.Add(new QueryToken(withEquals, text.Substring(pos, 2), pos, 2));
            return pos + 2;
        }
        tokens.Add(new QueryToken(bare, text[pos].ToString(), pos, 1));
        return pos + 1;
    }

    private int ReadBang(string text, int pos, List<QueryToken> tokens, List<QueryError> errors)
    {
        if (pos + 1 < text.Length && text[pos + 1] == '=')
        {
            tokens.Add(new QueryToken(QueryTokenKind.NotEquals, "!=", pos, 2));
            return pos + 2;
        }
        if (pos + 1 < text.Length && text[pos + 1] == '~')
        {
            tokens.Add(new QueryToken(QueryTokenKind.NotContains, "!~", pos, 2));
            return pos + 2;
        }
        errors.Add(new QueryError(QueryErrorCode.UnexpectedCharacter, pos, 1, "!"));
        return pos + 1;
    }

    private int ReadString(string text, int start, List<QueryToken> tokens, List<QueryError> errors)
    {
        var content = new StringBuilder();
        var pos = start + 1;
        while (pos < text.Length)
        {
            var c = text[pos];
            if (c == '\\' && pos + 1 < text.Length && (text[pos + 1] == '"' || text[pos + 1] == '\\'))
            {
                content.Append(text[pos + 1]);
                pos += 2;
                continue;
            }
            if (c == '"')
            {
                tokens.Add(new QueryToken(QueryTokenKind.String, content.ToString(), start, pos - start + 1));
                return pos + 1;
            }
            content.Append(c);
            pos++;
        }
        errors.Add(new QueryError(QueryErrorCode.UnterminatedString, start, text.Length - start));
        return text.Length;
    }

    private int ReadWord(string text, int start, List<QueryToken> tokens)
    {
        var pos = start;
        while (pos < text.Length && !char.IsWhiteSpace(text[pos]) && !IsSpecial(text[pos]))
            pos++;
        tokens.Add(new QueryToken(QueryTokenKind.Word, text[start..pos], start, pos - start));
        return pos;
    }

    private bool IsSpecial(char c) => c is '=' or '!' or '<' or '>' or '~' or '(' or ')' or ',' or '"';
}
