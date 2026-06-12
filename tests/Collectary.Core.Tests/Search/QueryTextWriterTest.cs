using Collectary.Core.Search;

namespace Collectary.Core.Tests.Search;

[TestFixture]
public class QueryTextWriterTest
{
    private QueryTextWriter _writer = null!;

    [SetUp]
    public void SetUp() => _writer = new QueryTextWriter();

    private static ConditionNode Condition(string field, QueryOperatorKind op, params string[] operands) => new()
    {
        Field = field,
        FieldStart = 0,
        FieldLength = 0,
        Operator = op,
        Operands = operands.Select(o => new QueryOperand(o, false, 0, 0)).ToList(),
    };

    private static ParsedQuery Query(QueryNode? root, params OrderByField[] orderBy) => new()
    {
        Root = root,
        OrderBy = orderBy,
    };

    [Test]
    public void Write_EverySymbolOperator_UsesItsSymbol()
    {
        Assert.That(_writer.Write(Query(Condition("a", QueryOperatorKind.Equals, "1"))), Is.EqualTo("a = 1"));
        Assert.That(_writer.Write(Query(Condition("a", QueryOperatorKind.NotEquals, "1"))), Is.EqualTo("a != 1"));
        Assert.That(_writer.Write(Query(Condition("a", QueryOperatorKind.Less, "1"))), Is.EqualTo("a < 1"));
        Assert.That(_writer.Write(Query(Condition("a", QueryOperatorKind.LessOrEqual, "1"))), Is.EqualTo("a <= 1"));
        Assert.That(_writer.Write(Query(Condition("a", QueryOperatorKind.Greater, "1"))), Is.EqualTo("a > 1"));
        Assert.That(_writer.Write(Query(Condition("a", QueryOperatorKind.GreaterOrEqual, "1"))), Is.EqualTo("a >= 1"));
        Assert.That(_writer.Write(Query(Condition("a", QueryOperatorKind.Contains, "x"))), Is.EqualTo("a ~ x"));
        Assert.That(_writer.Write(Query(Condition("a", QueryOperatorKind.NotContains, "x"))), Is.EqualTo("a !~ x"));
    }

    [Test]
    public void Write_WordOperators_UseLowercaseKeywords()
    {
        Assert.That(_writer.Write(Query(Condition("a", QueryOperatorKind.In, "1", "3"))), Is.EqualTo("a in (1, 3)"));
        Assert.That(_writer.Write(Query(Condition("a", QueryOperatorKind.IsEmpty))), Is.EqualTo("a is empty"));
        Assert.That(_writer.Write(Query(Condition("a", QueryOperatorKind.IsNotEmpty))), Is.EqualTo("a is not empty"));
    }

    [Test]
    public void Write_AndOrNot_UseUppercaseKeywords()
    {
        var and = new AndNode(Condition("a", QueryOperatorKind.Equals, "1"), Condition("b", QueryOperatorKind.Equals, "2"));
        Assert.That(_writer.Write(Query(and)), Is.EqualTo("a = 1 AND b = 2"));

        var or = new OrNode(Condition("a", QueryOperatorKind.Equals, "1"), Condition("b", QueryOperatorKind.Equals, "2"));
        Assert.That(_writer.Write(Query(or)), Is.EqualTo("a = 1 OR b = 2"));

        var not = new NotNode(Condition("a", QueryOperatorKind.Equals, "1"));
        Assert.That(_writer.Write(Query(not)), Is.EqualTo("NOT a = 1"));
    }

    [Test]
    public void Write_OrInsideAnd_GetsParentheses()
    {
        var tree = new AndNode(
            new OrNode(Condition("a", QueryOperatorKind.Equals, "1"), Condition("b", QueryOperatorKind.Equals, "2")),
            Condition("c", QueryOperatorKind.Equals, "3"));

        Assert.That(_writer.Write(Query(tree)), Is.EqualTo("(a = 1 OR b = 2) AND c = 3"));
    }

    [Test]
    public void Write_AndInsideOr_NeedsNoParentheses()
    {
        var tree = new OrNode(
            Condition("a", QueryOperatorKind.Equals, "1"),
            new AndNode(Condition("b", QueryOperatorKind.Equals, "2"), Condition("c", QueryOperatorKind.Equals, "3")));

        Assert.That(_writer.Write(Query(tree)), Is.EqualTo("a = 1 OR b = 2 AND c = 3"));
    }

    [Test]
    public void Write_NotOverCompoundOperand_GetsParentheses()
    {
        var tree = new NotNode(
            new AndNode(Condition("a", QueryOperatorKind.Equals, "1"), Condition("b", QueryOperatorKind.Equals, "2")));

        Assert.That(_writer.Write(Query(tree)), Is.EqualTo("NOT (a = 1 AND b = 2)"));
    }

    [Test]
    public void Write_OrderBy_OmitsAscAndWritesDesc()
    {
        var query = Query(Condition("a", QueryOperatorKind.Equals, "1"),
            new OrderByField("Name", false, 0, 0));
        Assert.That(_writer.Write(query), Is.EqualTo("a = 1 ORDER BY Name"));

        var desc = Query(Condition("a", QueryOperatorKind.Equals, "1"),
            new OrderByField("Name", true, 0, 0), new OrderByField("Price", false, 0, 0));
        Assert.That(_writer.Write(desc), Is.EqualTo("a = 1 ORDER BY Name DESC, Price"));
    }

    [Test]
    public void Write_OrderByWithoutFilter_WritesOnlyTheOrderClause()
    {
        var query = Query(null, new OrderByField("Name", false, 0, 0));

        Assert.That(_writer.Write(query), Is.EqualTo("ORDER BY Name"));
    }

    [Test]
    public void Write_SymbolConditionWithoutOperands_WritesAnEmptyQuotedValue()
    {
        Assert.That(_writer.Write(Query(Condition("a", QueryOperatorKind.Equals))), Is.EqualTo("a = \"\""));
    }

    [Test]
    public void Write_EmptyQuery_ReturnsEmptyString()
    {
        Assert.That(_writer.Write(Query(null)), Is.EqualTo(""));
    }

    [Test]
    public void WriteValue_BareWordsStayBare()
    {
        Assert.That(_writer.WriteValue("open"), Is.EqualTo("open"));
        Assert.That(_writer.WriteValue("2025-01-01"), Is.EqualTo("2025-01-01"));
        Assert.That(_writer.WriteValue("1.5"), Is.EqualTo("1.5"));
        Assert.That(_writer.WriteValue("-3"), Is.EqualTo("-3"));
    }

    [Test]
    public void WriteValue_QuotesWhenNeeded()
    {
        Assert.That(_writer.WriteValue("two words"), Is.EqualTo("\"two words\""));
        Assert.That(_writer.WriteValue(""), Is.EqualTo("\"\""));
        Assert.That(_writer.WriteValue("a=b"), Is.EqualTo("\"a=b\""));
        Assert.That(_writer.WriteValue("a(b"), Is.EqualTo("\"a(b\""));
        Assert.That(_writer.WriteValue("a,b"), Is.EqualTo("\"a,b\""));
        Assert.That(_writer.WriteValue("a~b"), Is.EqualTo("\"a~b\""));
        Assert.That(_writer.WriteValue("a!b"), Is.EqualTo("\"a!b\""));
        Assert.That(_writer.WriteValue("a<b"), Is.EqualTo("\"a<b\""));
        Assert.That(_writer.WriteValue("a>b"), Is.EqualTo("\"a>b\""));
    }

    [Test]
    public void WriteValue_QuotesReservedWordsCaseInsensitively()
    {
        Assert.That(_writer.WriteValue("and"), Is.EqualTo("\"and\""));
        Assert.That(_writer.WriteValue("OR"), Is.EqualTo("\"OR\""));
        Assert.That(_writer.WriteValue("Not"), Is.EqualTo("\"Not\""));
        Assert.That(_writer.WriteValue("in"), Is.EqualTo("\"in\""));
        Assert.That(_writer.WriteValue("is"), Is.EqualTo("\"is\""));
        Assert.That(_writer.WriteValue("empty"), Is.EqualTo("\"empty\""));
        Assert.That(_writer.WriteValue("order"), Is.EqualTo("\"order\""));
        Assert.That(_writer.WriteValue("by"), Is.EqualTo("\"by\""));
        Assert.That(_writer.WriteValue("asc"), Is.EqualTo("\"asc\""));
        Assert.That(_writer.WriteValue("DESC"), Is.EqualTo("\"DESC\""));
    }

    [Test]
    public void WriteValue_EscapesBackslashesAndQuotes()
    {
        Assert.That(_writer.WriteValue("a \"b\" c"), Is.EqualTo("\"a \\\"b\\\" c\""));
        Assert.That(_writer.WriteValue("c:\\temp"), Is.EqualTo("\"c:\\\\temp\""));
    }

    [Test]
    public void Write_QuotedFieldLabelsAndValues_SurviveAParseRoundTrip()
    {
        var query = Query(Condition("Print run", QueryOperatorKind.Equals, "two words"),
            new OrderByField("Print run", true, 0, 0));

        var text = _writer.Write(query);

        Assert.That(text, Is.EqualTo("\"Print run\" = \"two words\" ORDER BY \"Print run\" DESC"));
        var reparsed = new QueryParser(new QueryLexer()).Parse(text);
        Assert.That(reparsed.Errors, Is.Empty);
        var condition = (ConditionNode)reparsed.Query!.Root!;
        Assert.That(condition.Field, Is.EqualTo("Print run"));
        Assert.That(condition.Operands.Single().Text, Is.EqualTo("two words"));
        Assert.That(reparsed.Query.OrderBy.Single().Field, Is.EqualTo("Print run"));
    }

    [TestCase("preset = \"abcd\" AND Price > 3 OR (x != \"test\" AND a in (1, 3)) ORDER BY Name")]
    [TestCase("a = 1 AND (b = 2 OR c = 3)")]
    [TestCase("NOT (a = 1 OR b = 2) AND c ~ x")]
    [TestCase("a is not empty ORDER BY b DESC")]
    public void Write_AfterParsing_IsIdempotentOnItsOwnOutput(string original)
    {
        var parser = new QueryParser(new QueryLexer());

        var canonical = _writer.Write(parser.Parse(original).Query!);
        var reparsed = parser.Parse(canonical);

        Assert.That(reparsed.Errors, Is.Empty);
        Assert.That(_writer.Write(reparsed.Query!), Is.EqualTo(canonical));
    }
}
