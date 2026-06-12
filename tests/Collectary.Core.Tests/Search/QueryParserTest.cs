using Collectary.Core.Search;

namespace Collectary.Core.Tests.Search;

[TestFixture]
public class QueryParserTest
{
    private QueryParser _parser = null!;

    [SetUp]
    public void SetUp() => _parser = new QueryParser(new QueryLexer());

    private ParsedQuery ParseOk(string text)
    {
        var result = _parser.Parse(text);
        Assert.That(result.Errors, Is.Empty, $"expected no errors for: {text}");
        Assert.That(result.Query, Is.Not.Null);
        return result.Query!;
    }

    private static ConditionNode Condition(QueryNode? node)
    {
        Assert.That(node, Is.InstanceOf<ConditionNode>());
        return (ConditionNode)node!;
    }

    [Test]
    public void Parse_SingleCondition_BuildsConditionNode()
    {
        var query = ParseOk("Status = open");

        var condition = Condition(query.Root);
        Assert.That(condition.Field, Is.EqualTo("Status"));
        Assert.That(condition.Operator, Is.EqualTo(QueryOperatorKind.Equals));
        Assert.That(condition.Operands.Single().Text, Is.EqualTo("open"));
        Assert.That(condition.FieldStart, Is.EqualTo(0));
        Assert.That(condition.FieldLength, Is.EqualTo(6));
    }

    [Test]
    public void Parse_QuotedFieldAndValue_PreserveInnerText()
    {
        var query = ParseOk("\"My Field\" = \"x y\"");

        var condition = Condition(query.Root);
        Assert.That(condition.Field, Is.EqualTo("My Field"));
        Assert.That(condition.Operands.Single().Text, Is.EqualTo("x y"));
        Assert.That(condition.Operands.Single().WasQuoted, Is.True);
    }

    [Test]
    public void Parse_OrBindsLooserThanAnd()
    {
        var query = ParseOk("a = 1 OR b = 2 AND c = 3");

        var or = (OrNode)query.Root!;
        Assert.That(Condition(or.Left).Field, Is.EqualTo("a"));
        var and = (AndNode)or.Right;
        Assert.That(Condition(and.Left).Field, Is.EqualTo("b"));
        Assert.That(Condition(and.Right).Field, Is.EqualTo("c"));
    }

    [Test]
    public void Parse_NotBindsTighterThanAnd()
    {
        var query = ParseOk("NOT a = 1 AND b = 2");

        var and = (AndNode)query.Root!;
        var not = (NotNode)and.Left;
        Assert.That(Condition(not.Operand).Field, Is.EqualTo("a"));
        Assert.That(Condition(and.Right).Field, Is.EqualTo("b"));
    }

    [Test]
    public void Parse_Parentheses_OverridePrecedence()
    {
        var query = ParseOk("(a = 1 OR b = 2) AND c = 3");

        var and = (AndNode)query.Root!;
        Assert.That(and.Left, Is.InstanceOf<OrNode>());
        Assert.That(Condition(and.Right).Field, Is.EqualTo("c"));
    }

    [Test]
    public void Parse_InList_CollectsAllOperands()
    {
        var query = ParseOk("a in (1, 3, \"x\")");

        var condition = Condition(query.Root);
        Assert.That(condition.Operator, Is.EqualTo(QueryOperatorKind.In));
        Assert.That(condition.Operands.Select(o => o.Text), Is.EqualTo(new[] { "1", "3", "x" }));
    }

    [Test]
    public void Parse_NotIn_WrapsConditionInNotNode()
    {
        var query = ParseOk("a not in (1, 2)");

        var not = (NotNode)query.Root!;
        Assert.That(Condition(not.Operand).Operator, Is.EqualTo(QueryOperatorKind.In));
    }

    [Test]
    public void Parse_IsEmptyAndIsNotEmpty_MapToOperatorsWithoutOperands()
    {
        var empty = Condition(ParseOk("a is empty").Root);
        Assert.That(empty.Operator, Is.EqualTo(QueryOperatorKind.IsEmpty));
        Assert.That(empty.Operands, Is.Empty);

        var notEmpty = Condition(ParseOk("a is not empty").Root);
        Assert.That(notEmpty.Operator, Is.EqualTo(QueryOperatorKind.IsNotEmpty));
    }

    [Test]
    public void Parse_ContainsOperators_MapToContainsKinds()
    {
        Assert.That(Condition(ParseOk("a ~ foo").Root).Operator, Is.EqualTo(QueryOperatorKind.Contains));
        Assert.That(Condition(ParseOk("a !~ foo").Root).Operator, Is.EqualTo(QueryOperatorKind.NotContains));
    }

    [Test]
    public void Parse_OrderBy_SupportsDirectionsAndMultipleFields()
    {
        var query = ParseOk("a = 1 ORDER BY Name DESC, Price, Added asc");

        Assert.That(query.OrderBy.Select(o => o.Field), Is.EqualTo(new[] { "Name", "Price", "Added" }));
        Assert.That(query.OrderBy.Select(o => o.Descending), Is.EqualTo(new[] { true, false, false }));
    }

    [Test]
    public void Parse_KeywordsAreCaseInsensitive()
    {
        var query = ParseOk("a = 1 aNd nOt b = 2 oR c In (1) Order By Name");

        Assert.That(query.Root, Is.InstanceOf<OrNode>());
        Assert.That(query.OrderBy.Single().Field, Is.EqualTo("Name"));
    }

    [Test]
    public void Parse_OwnersExample_BuildsExpectedShape()
    {
        var query = ParseOk(
            "preset = \"abcd\" AND Price > 3 OR (x != \"test\" AND a in (1,3)) Order By Name");

        var or = (OrNode)query.Root!;
        var leftAnd = (AndNode)or.Left;
        Assert.That(Condition(leftAnd.Left).Field, Is.EqualTo("preset"));
        Assert.That(Condition(leftAnd.Right).Operator, Is.EqualTo(QueryOperatorKind.Greater));
        var rightAnd = (AndNode)or.Right;
        Assert.That(Condition(rightAnd.Left).Operator, Is.EqualTo(QueryOperatorKind.NotEquals));
        Assert.That(Condition(rightAnd.Right).Operands, Has.Count.EqualTo(2));
        Assert.That(query.OrderBy.Single().Field, Is.EqualTo("Name"));
    }

    [Test]
    public void Parse_EmptyInput_YieldsMatchAllQuery()
    {
        Assert.That(ParseOk("").Root, Is.Null);
        Assert.That(ParseOk("   ").Root, Is.Null);
    }

    [Test]
    public void Parse_OnlyOrderBy_YieldsMatchAllWithSort()
    {
        var query = ParseOk("ORDER BY Name");

        Assert.That(query.Root, Is.Null);
        Assert.That(query.OrderBy.Single().Field, Is.EqualTo("Name"));
    }

    [Test]
    public void Parse_MissingValue_ReportsExpectedValueAtEnd()
    {
        var result = _parser.Parse("a =");

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.ExpectedValue));
        Assert.That(result.Errors.Single().Start, Is.EqualTo(3));
    }

    [Test]
    public void Parse_WordAfterField_ReportsExpectedOperator()
    {
        var result = _parser.Parse("a b");

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.ExpectedOperator));
        Assert.That(result.Errors.Single().Start, Is.EqualTo(2));
    }

    [Test]
    public void Parse_UnclosedParenthesis_ReportsExpectedClosingParen()
    {
        var result = _parser.Parse("(a = 1");

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.ExpectedClosingParen));
    }

    [Test]
    public void Parse_MissingConnective_ReportsUnexpectedToken()
    {
        var result = _parser.Parse("a = 1 b = 2");

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.UnexpectedToken));
        Assert.That(result.Errors.Single().Start, Is.EqualTo(6));
    }

    [Test]
    public void Parse_OrderByWithoutField_ReportsExpectedOrderField()
    {
        var result = _parser.Parse("a = 1 ORDER BY");

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.ExpectedOrderField));
    }

    [Test]
    public void Parse_LexerError_FlowsThroughAsParseError()
    {
        var result = _parser.Parse("a = \"open");

        Assert.That(result.Errors, Has.Some.Matches<QueryError>(
            e => e.Code == QueryErrorCode.UnterminatedString));
    }

    [Test]
    public void Parse_OperatorInFieldPosition_ReportsExpectedField()
    {
        var result = _parser.Parse("( = 3 )");

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.ExpectedField));
    }

    [Test]
    public void Parse_IsFollowedByGarbage_ReportsUnexpectedToken()
    {
        var result = _parser.Parse("a is open");

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.UnexpectedToken));
    }

    [Test]
    public void Parse_NotWithoutIn_ReportsExpectedOperator()
    {
        var result = _parser.Parse("a not 5");

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.ExpectedOperator));
    }

    [Test]
    public void Parse_InWithoutParenthesis_ReportsExpectedValue()
    {
        var result = _parser.Parse("a in 5");

        Assert.That(result.Errors.Single().Code, Is.EqualTo(QueryErrorCode.ExpectedValue));
    }
}
