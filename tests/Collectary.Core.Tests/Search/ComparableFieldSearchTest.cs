using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Search;

namespace Collectary.Core.Tests.Search;

[TestFixture]
public class ComparableFieldSearchTest
{
    private static readonly Guid DefinitionId = Guid.NewGuid();

    private ComparableFieldSearch<IntegerFieldValue, int> _search = null!;

    [SetUp]
    public void SetUp() => _search = new ComparableFieldSearch<IntegerFieldValue, int>(
        v => v.Value,
        v => v.Value,
        raw => int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null);

    private static Item ItemWithNumber(int? number) => new()
    {
        Values = [new IntegerFieldValue { FieldDefinitionId = DefinitionId, Value = number }],
    };

    private IFieldConditionMatcher Matcher(QueryOperatorKind op, params string[] operands)
    {
        Assert.That(_search.TryCreateMatcher(op, operands, out var matcher, out _), Is.True);
        return matcher!;
    }

    private ComparableFieldSearch<WeightFieldValue, decimal> UnitGuardedSearch() => new(
        v => v.Amount,
        v => v.Amount,
        raw => decimal.TryParse(raw.Split(' ')[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null,
        operandConstraint: raw => raw.Split(' ') is [_, var unit]
            ? v => string.Equals(v.Unit, unit, StringComparison.OrdinalIgnoreCase)
            : null);

    private static Item ItemWithWeight(decimal amount, string unit) => new()
    {
        Values = [new WeightFieldValue { FieldDefinitionId = DefinitionId, Amount = amount, Unit = unit }],
    };

    [Test]
    public void OperandConstraint_PositiveOperators_RequireTheConstraint()
    {
        var search = UnitGuardedSearch();
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["500 g"], out var equals, out _), Is.True);
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Greater, ["1 kg"], out var greater, out _), Is.True);

        Assert.That(equals!.Matches(ItemWithWeight(500, "g"), [DefinitionId]), Is.True);
        Assert.That(equals.Matches(ItemWithWeight(500, "kg"), [DefinitionId]), Is.False);
        Assert.That(greater!.Matches(ItemWithWeight(2, "kg"), [DefinitionId]), Is.True);
        Assert.That(greater.Matches(ItemWithWeight(500, "g"), [DefinitionId]), Is.False);
    }

    [Test]
    public void OperandConstraint_WithoutAConstrainedOperand_MatchesAcrossAllValues()
    {
        var search = UnitGuardedSearch();
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["500"], out var matcher, out _), Is.True);

        Assert.That(matcher!.Matches(ItemWithWeight(500, "g"), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithWeight(500, "kg"), [DefinitionId]), Is.True);
    }

    [Test]
    public void OperandConstraint_NotEquals_MatchesOtherUnitsAndDropsTheServerFilter()
    {
        var search = UnitGuardedSearch();
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.NotEquals, ["500 g"], out var matcher, out _), Is.True);

        Assert.That(matcher!.Matches(ItemWithWeight(500, "kg"), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithWeight(500, "g"), [DefinitionId]), Is.False);
        Assert.That(matcher.Matches(ItemWithWeight(400, "g"), [DefinitionId]), Is.True);
        Assert.That(matcher.ServerFilter([DefinitionId]), Is.Null);
    }

    [Test]
    public void OperandConstraint_In_GuardsEachOperandSeparately()
    {
        var search = UnitGuardedSearch();
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.In, ["500 g", "3 kg"], out var matcher, out _), Is.True);

        Assert.That(matcher!.Matches(ItemWithWeight(500, "g"), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithWeight(3, "kg"), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithWeight(500, "kg"), [DefinitionId]), Is.False);
        Assert.That(matcher.Matches(ItemWithWeight(3, "g"), [DefinitionId]), Is.False);
    }

    [Test]
    public void OperandConstraint_PositiveOperators_KeepTheAmountServerFilterAsASuperset()
    {
        var search = UnitGuardedSearch();
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["500 g"], out var matcher, out _), Is.True);

        var filter = matcher!.ServerFilter([DefinitionId]);
        Assert.That(filter, Is.Not.Null);
        Assert.That(filter!.Compile()(ItemWithWeight(500, "kg")), Is.True);
        Assert.That(filter.Compile()(ItemWithWeight(400, "g")), Is.False);
    }

    [Test]
    public void Operators_CoverOrderedComparisonsAndEmptiness()
    {
        Assert.That(_search.Operators, Is.EquivalentTo(new[]
        {
            QueryOperatorKind.Equals, QueryOperatorKind.NotEquals,
            QueryOperatorKind.Less, QueryOperatorKind.LessOrEqual,
            QueryOperatorKind.Greater, QueryOperatorKind.GreaterOrEqual,
            QueryOperatorKind.In, QueryOperatorKind.IsEmpty, QueryOperatorKind.IsNotEmpty,
        }));
    }

    [Test]
    public void ComparisonOperators_MatchAgainstParsedOperand()
    {
        Assert.That(Matcher(QueryOperatorKind.Equals, "5")
            .Matches(ItemWithNumber(5), [DefinitionId]), Is.True);
        Assert.That(Matcher(QueryOperatorKind.Greater, "5")
            .Matches(ItemWithNumber(6), [DefinitionId]), Is.True);
        Assert.That(Matcher(QueryOperatorKind.Greater, "5")
            .Matches(ItemWithNumber(5), [DefinitionId]), Is.False);
        Assert.That(Matcher(QueryOperatorKind.LessOrEqual, "5")
            .Matches(ItemWithNumber(5), [DefinitionId]), Is.True);
        Assert.That(Matcher(QueryOperatorKind.Less, "5")
            .Matches(ItemWithNumber(5), [DefinitionId]), Is.False);
        Assert.That(Matcher(QueryOperatorKind.GreaterOrEqual, "5")
            .Matches(ItemWithNumber(4), [DefinitionId]), Is.False);
    }

    [Test]
    public void NotEquals_RequiresAPresentDifferentValue()
    {
        var matcher = Matcher(QueryOperatorKind.NotEquals, "5");

        Assert.That(matcher.Matches(ItemWithNumber(4), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithNumber(5), [DefinitionId]), Is.False);
        Assert.That(matcher.Matches(ItemWithNumber(null), [DefinitionId]), Is.False);
        Assert.That(matcher.Matches(new Item(), [DefinitionId]), Is.False);
    }

    [Test]
    public void In_MatchesAnyListedNumber()
    {
        var matcher = Matcher(QueryOperatorKind.In, "1", "3");

        Assert.That(matcher.Matches(ItemWithNumber(3), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithNumber(2), [DefinitionId]), Is.False);
    }

    [Test]
    public void UnparsableOperand_ReportsInvalidValue()
    {
        Assert.That(_search.TryCreateMatcher(QueryOperatorKind.Equals, ["abc"], out _, out var error), Is.False);
        Assert.That(error, Is.EqualTo(QueryErrorCode.InvalidValue));
    }

    [Test]
    public void UnorderedMode_RejectsRelationalOperators()
    {
        var unordered = new ComparableFieldSearch<IntegerFieldValue, int>(
            v => v.Value, v => v.Value, _ => 1, ordered: false);

        Assert.That(unordered.Operators, Does.Not.Contain(QueryOperatorKind.Greater));
        Assert.That(unordered.TryCreateMatcher(QueryOperatorKind.Greater, ["1"], out _, out var error), Is.False);
        Assert.That(error, Is.EqualTo(QueryErrorCode.OperatorNotSupported));
    }

    [Test]
    public void ServerFilter_Greater_CompilesToTheSamePredicate()
    {
        var filter = Matcher(QueryOperatorKind.Greater, "5").ServerFilter([DefinitionId]);

        Assert.That(filter, Is.Not.Null);
        var compiled = filter!.Compile();
        Assert.That(compiled(ItemWithNumber(6)), Is.True);
        Assert.That(compiled(ItemWithNumber(5)), Is.False);
        Assert.That(compiled(ItemWithNumber(null)), Is.False);
    }

    [Test]
    public void ServerFilter_NotEqualsAndIn_CompileToTheSamePredicates()
    {
        var notEquals = Matcher(QueryOperatorKind.NotEquals, "5").ServerFilter([DefinitionId])!.Compile();
        Assert.That(notEquals(ItemWithNumber(4)), Is.True);
        Assert.That(notEquals(ItemWithNumber(null)), Is.False);

        var inFilter = Matcher(QueryOperatorKind.In, "1", "3").ServerFilter([DefinitionId])!.Compile();
        Assert.That(inFilter(ItemWithNumber(3)), Is.True);
        Assert.That(inFilter(ItemWithNumber(2)), Is.False);
    }

    [Test]
    public void SortKey_ReturnsTheUnderlyingNumber()
    {
        Assert.That(_search.SortKey(new Item(), new IntegerFieldValue { Value = 7 }), Is.EqualTo(7));
        Assert.That(_search.SortKey(new Item(), new IntegerFieldValue()), Is.Null);
        Assert.That(_search.SortKey(new Item(), null), Is.Null);
    }

    [Test]
    public void EmptinessOperators_MatchOnValuePresence()
    {
        Assert.That(Matcher(QueryOperatorKind.IsEmpty).Matches(new Item(), [DefinitionId]), Is.True);
        Assert.That(Matcher(QueryOperatorKind.IsEmpty).Matches(ItemWithNumber(1), [DefinitionId]), Is.False);
        Assert.That(Matcher(QueryOperatorKind.IsNotEmpty).Matches(ItemWithNumber(1), [DefinitionId]), Is.True);
    }

    [Test]
    public void In_WithUnparsableEntry_ReportsInvalidValue()
    {
        Assert.That(_search.TryCreateMatcher(QueryOperatorKind.In, ["1", "abc"], out _, out var error), Is.False);
        Assert.That(error, Is.EqualTo(QueryErrorCode.InvalidValue));
    }

    [Test]
    public void ServerFilter_EveryRelationalOperator_AgreesWithTheMemoryPredicate()
    {
        var operators = new[]
        {
            QueryOperatorKind.Equals, QueryOperatorKind.NotEquals,
            QueryOperatorKind.Less, QueryOperatorKind.LessOrEqual,
            QueryOperatorKind.Greater, QueryOperatorKind.GreaterOrEqual,
        };
        foreach (var op in operators)
        {
            var matcher = Matcher(op, "5");
            var compiled = matcher.ServerFilter([DefinitionId])!.Compile();
            foreach (var number in new int?[] { 4, 5, 6, null })
            {
                var item = ItemWithNumber(number);
                Assert.That(compiled(item), Is.EqualTo(matcher.Matches(item, [DefinitionId])),
                    $"{op} on {number?.ToString() ?? "null"} must agree between SQL and memory");
            }
        }
    }
}
