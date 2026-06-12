using Collectary.Core.Search;

namespace Collectary.Core.Tests.Search;

[TestFixture]
public class QueryLexerTest
{
    private QueryLexer _lexer = null!;

    [SetUp]
    public void SetUp() => _lexer = new QueryLexer();

    [Test]
    public void Tokenize_SimpleCondition_ProducesWordOperatorWord()
    {
        var result = _lexer.Tokenize("Status = open");

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Tokens.Select(t => t.Kind), Is.EqualTo(new[]
        {
            QueryTokenKind.Word, QueryTokenKind.Equals, QueryTokenKind.Word, QueryTokenKind.End,
        }));
        Assert.That(result.Tokens[0].Text, Is.EqualTo("Status"));
        Assert.That(result.Tokens[0].Start, Is.EqualTo(0));
        Assert.That(result.Tokens[0].Length, Is.EqualTo(6));
        Assert.That(result.Tokens[2].Text, Is.EqualTo("open"));
        Assert.That(result.Tokens[2].Start, Is.EqualTo(9));
    }

    [Test]
    public void Tokenize_QuotedString_UnescapesContentAndSpansIncludeQuotes()
    {
        var result = _lexer.Tokenize("name = \"a \\\"b\\\" c\"");

        Assert.That(result.Errors, Is.Empty);
        var str = result.Tokens[2];
        Assert.That(str.Kind, Is.EqualTo(QueryTokenKind.String));
        Assert.That(str.Text, Is.EqualTo("a \"b\" c"));
        Assert.That(str.Start, Is.EqualTo(7));
        Assert.That(str.Length, Is.EqualTo(11));
    }

    [Test]
    public void Tokenize_AllComparisonOperators_ProduceDistinctKinds()
    {
        var result = _lexer.Tokenize("= != < <= > >= ~ !~");

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Tokens.Select(t => t.Kind), Is.EqualTo(new[]
        {
            QueryTokenKind.Equals, QueryTokenKind.NotEquals,
            QueryTokenKind.Less, QueryTokenKind.LessOrEqual,
            QueryTokenKind.Greater, QueryTokenKind.GreaterOrEqual,
            QueryTokenKind.Contains, QueryTokenKind.NotContains,
            QueryTokenKind.End,
        }));
    }

    [Test]
    public void Tokenize_ParensAndCommas_ProduceStructuralTokens()
    {
        var result = _lexer.Tokenize("in (1,3)");

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Tokens.Select(t => t.Kind), Is.EqualTo(new[]
        {
            QueryTokenKind.Word, QueryTokenKind.OpenParen, QueryTokenKind.Word,
            QueryTokenKind.Comma, QueryTokenKind.Word, QueryTokenKind.CloseParen, QueryTokenKind.End,
        }));
        Assert.That(result.Tokens[2].Text, Is.EqualTo("1"));
        Assert.That(result.Tokens[4].Text, Is.EqualTo("3"));
    }

    [Test]
    public void Tokenize_UnterminatedString_ReportsErrorWithSpan()
    {
        var result = _lexer.Tokenize("a = \"open");

        Assert.That(result.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo(QueryErrorCode.UnterminatedString));
        Assert.That(result.Errors[0].Start, Is.EqualTo(4));
        Assert.That(result.Errors[0].Length, Is.EqualTo(5));
    }

    [Test]
    public void Tokenize_BangWithoutComparator_ReportsUnexpectedCharacter()
    {
        var result = _lexer.Tokenize("a ! b");

        Assert.That(result.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo(QueryErrorCode.UnexpectedCharacter));
        Assert.That(result.Errors[0].Start, Is.EqualTo(2));
    }

    [Test]
    public void Tokenize_EmptyAndWhitespaceInput_ReturnsOnlyEndToken()
    {
        Assert.That(_lexer.Tokenize("").Tokens.Select(t => t.Kind), Is.EqualTo(new[] { QueryTokenKind.End }));
        Assert.That(_lexer.Tokenize("   ").Tokens.Select(t => t.Kind), Is.EqualTo(new[] { QueryTokenKind.End }));
    }

    [Test]
    public void Tokenize_BareWordsKeepDatesNumbersAndSymbols()
    {
        var result = _lexer.Tokenize("2025-01-01 1.5 a_b@c -3");

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Tokens.Where(t => t.Kind == QueryTokenKind.Word).Select(t => t.Text),
            Is.EqualTo(new[] { "2025-01-01", "1.5", "a_b@c", "-3" }));
    }

    [Test]
    public void Tokenize_EndToken_SitsAtTextLength()
    {
        var result = _lexer.Tokenize("abc");

        Assert.That(result.Tokens[^1].Kind, Is.EqualTo(QueryTokenKind.End));
        Assert.That(result.Tokens[^1].Start, Is.EqualTo(3));
        Assert.That(result.Tokens[^1].Length, Is.EqualTo(0));
    }

    [Test]
    public void Tokenize_ComparatorsAtEndOfInput_StayBare()
    {
        Assert.That(_lexer.Tokenize("a <").Tokens[1].Kind, Is.EqualTo(QueryTokenKind.Less));
        Assert.That(_lexer.Tokenize("a >").Tokens[1].Kind, Is.EqualTo(QueryTokenKind.Greater));
    }

    [Test]
    public void Tokenize_BangAtEndOfInput_ReportsUnexpectedCharacter()
    {
        var result = _lexer.Tokenize("a !");

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.UnexpectedCharacter));
    }

    [Test]
    public void Tokenize_BackslashAtEndOfUnterminatedString_IsStillUnterminated()
    {
        var result = _lexer.Tokenize("a = \"x\\");

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.UnterminatedString));
    }

    [Test]
    public void Tokenize_WordsStopAtEverySpecialCharacter()
    {
        Assert.That(_lexer.Tokenize("a=b").Tokens.Select(t => t.Kind), Is.EqualTo(new[]
        {
            QueryTokenKind.Word, QueryTokenKind.Equals, QueryTokenKind.Word, QueryTokenKind.End,
        }));
        Assert.That(_lexer.Tokenize("a~b").Tokens[1].Kind, Is.EqualTo(QueryTokenKind.Contains));
        Assert.That(_lexer.Tokenize("a<b").Tokens[1].Kind, Is.EqualTo(QueryTokenKind.Less));
        Assert.That(_lexer.Tokenize("a>b").Tokens[1].Kind, Is.EqualTo(QueryTokenKind.Greater));
        Assert.That(_lexer.Tokenize("a\"b\"").Tokens.Select(t => t.Kind), Is.EqualTo(new[]
        {
            QueryTokenKind.Word, QueryTokenKind.String, QueryTokenKind.End,
        }));
    }
}
