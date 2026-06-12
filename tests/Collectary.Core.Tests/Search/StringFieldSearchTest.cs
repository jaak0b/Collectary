using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Search;

[TestFixture]
public class StringFieldSearchTest
{
    private static readonly Guid DefinitionId = Guid.NewGuid();

    private StringFieldSearch<TextFieldValue> _search = null!;

    [SetUp]
    public void SetUp() => _search = new StringFieldSearch<TextFieldValue>(v => v.Value, v => v.Value);

    private static Item ItemWithText(string? text, Guid? definitionId = null) => new()
    {
        Values = [new TextFieldValue { FieldDefinitionId = definitionId ?? DefinitionId, Value = text }],
    };

    private IFieldConditionMatcher Matcher(QueryOperatorKind op, params string[] operands)
    {
        Assert.That(_search.TryCreateMatcher(op, operands, out var matcher, out _), Is.True);
        return matcher!;
    }

    [Test]
    public void Operators_CoverStringComparisonsAndEmptiness()
    {
        Assert.That(_search.Operators, Is.EquivalentTo(new[]
        {
            QueryOperatorKind.Equals, QueryOperatorKind.NotEquals,
            QueryOperatorKind.Contains, QueryOperatorKind.NotContains,
            QueryOperatorKind.In, QueryOperatorKind.IsEmpty, QueryOperatorKind.IsNotEmpty,
        }));
    }

    [Test]
    public void Equals_MatchesAsciiCaseInsensitively()
    {
        var matcher = Matcher(QueryOperatorKind.Equals, "open");

        Assert.That(matcher.Matches(ItemWithText("OPEN"), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithText("closed"), [DefinitionId]), Is.False);
    }

    [Test]
    public void Equals_IgnoresValuesOfOtherDefinitions()
    {
        var matcher = Matcher(QueryOperatorKind.Equals, "open");

        Assert.That(matcher.Matches(ItemWithText("open", Guid.NewGuid()), [DefinitionId]), Is.False);
    }

    [Test]
    public void NotEquals_RequiresAPresentDifferentValue()
    {
        var matcher = Matcher(QueryOperatorKind.NotEquals, "open");

        Assert.That(matcher.Matches(ItemWithText("closed"), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithText("open"), [DefinitionId]), Is.False);
        Assert.That(matcher.Matches(new Item(), [DefinitionId]), Is.False);
    }

    [Test]
    public void ContainsAndNotContains_MatchSubstrings()
    {
        Assert.That(Matcher(QueryOperatorKind.Contains, "pen")
            .Matches(ItemWithText("Open"), [DefinitionId]), Is.True);
        Assert.That(Matcher(QueryOperatorKind.Contains, "pen")
            .Matches(ItemWithText("shut"), [DefinitionId]), Is.False);
        Assert.That(Matcher(QueryOperatorKind.NotContains, "pen")
            .Matches(ItemWithText("shut"), [DefinitionId]), Is.True);
        Assert.That(Matcher(QueryOperatorKind.NotContains, "pen")
            .Matches(new Item(), [DefinitionId]), Is.False);
    }

    [Test]
    public void In_MatchesAnyListedValue()
    {
        var matcher = Matcher(QueryOperatorKind.In, "a", "B");

        Assert.That(matcher.Matches(ItemWithText("b"), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithText("c"), [DefinitionId]), Is.False);
    }

    [Test]
    public void IsEmpty_MatchesMissingAndBlankValues()
    {
        var matcher = Matcher(QueryOperatorKind.IsEmpty);

        Assert.That(matcher.Matches(new Item(), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithText(""), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithText("x"), [DefinitionId]), Is.False);

        var notEmpty = Matcher(QueryOperatorKind.IsNotEmpty);
        Assert.That(notEmpty.Matches(ItemWithText("x"), [DefinitionId]), Is.True);
        Assert.That(notEmpty.Matches(new Item(), [DefinitionId]), Is.False);
    }

    [Test]
    public void UnsupportedOperator_ReportsOperatorNotSupported()
    {
        Assert.That(_search.TryCreateMatcher(QueryOperatorKind.Greater, ["1"], out _, out var error), Is.False);
        Assert.That(error, Is.EqualTo(QueryErrorCode.OperatorNotSupported));
    }

    [Test]
    public void ServerFilter_Equals_CompilesToTheSamePredicate()
    {
        var matcher = Matcher(QueryOperatorKind.Equals, "Open");
        var filter = matcher.ServerFilter([DefinitionId]);

        Assert.That(filter, Is.Not.Null);
        var compiled = filter!.Compile();
        Assert.That(compiled(ItemWithText("open")), Is.True);
        Assert.That(compiled(ItemWithText("closed")), Is.False);
        Assert.That(compiled(new Item()), Is.False);
    }

    [Test]
    public void ServerFilter_ContainsAndIn_CompileToTheSamePredicates()
    {
        var contains = Matcher(QueryOperatorKind.Contains, "pen").ServerFilter([DefinitionId])!.Compile();
        Assert.That(contains(ItemWithText("open")), Is.True);
        Assert.That(contains(ItemWithText("shut")), Is.False);

        var inFilter = Matcher(QueryOperatorKind.In, "a", "B").ServerFilter([DefinitionId])!.Compile();
        Assert.That(inFilter(ItemWithText("b")), Is.True);
        Assert.That(inFilter(ItemWithText("c")), Is.False);
    }

    [Test]
    public void SortKey_ReturnsTheUnderlyingString()
    {
        var value = new TextFieldValue { Value = "abc" };

        Assert.That(_search.SortKey(new Item(), value), Is.EqualTo("abc"));
        Assert.That(_search.SortKey(new Item(), null), Is.Null);
    }

    [Test]
    public void EmptinessMatchers_AreNeverPushedToTheServer()
    {
        Assert.That(Matcher(QueryOperatorKind.IsEmpty).ServerFilter([DefinitionId]), Is.Null);
        Assert.That(Matcher(QueryOperatorKind.IsNotEmpty).ServerFilter([DefinitionId]), Is.Null);
    }
}
