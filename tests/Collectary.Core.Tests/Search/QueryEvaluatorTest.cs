using System.Linq.Expressions;
using Collectary.Core.Domain;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Search;

[TestFixture]
public class QueryEvaluatorTest
{
    private QueryEvaluator _evaluator = null!;

    [SetUp]
    public void SetUp() => _evaluator = new QueryEvaluator();

    private sealed class FixedMatcher : IFieldConditionMatcher
    {
        private readonly Func<Item, bool> _predicate;

        public FixedMatcher(bool result) : this(_ => result) { }

        public FixedMatcher(Func<Item, bool> predicate) => _predicate = predicate;

        public Expression<Func<Item, bool>>? ServerFilter(IReadOnlyCollection<Guid> definitionIds) => null;

        public bool Matches(Item item, IReadOnlyCollection<Guid> definitionIds) => _predicate(item);
    }

    private static BoundConditionNode Condition(QueryOperatorKind op, params bool[] bindingResults) =>
        new()
        {
            Operator = op,
            Bindings = bindingResults
                .Select(r => new BoundFieldMatch(new FixedMatcher(r), Array.Empty<Guid>()))
                .ToList(),
        };

    [Test]
    public void Matches_NullRoot_MatchesEverything()
    {
        Assert.That(_evaluator.Matches(null, new Item()), Is.True);
    }

    [Test]
    public void Matches_BooleanComposition_EvaluatesAndOrNot()
    {
        var yes = Condition(QueryOperatorKind.Equals, true);
        var no = Condition(QueryOperatorKind.Equals, false);

        Assert.That(_evaluator.Matches(new BoundAndNode(yes, yes), new Item()), Is.True);
        Assert.That(_evaluator.Matches(new BoundAndNode(yes, no), new Item()), Is.False);
        Assert.That(_evaluator.Matches(new BoundOrNode(no, yes), new Item()), Is.True);
        Assert.That(_evaluator.Matches(new BoundOrNode(no, no), new Item()), Is.False);
        Assert.That(_evaluator.Matches(new BoundNotNode(no), new Item()), Is.True);
        Assert.That(_evaluator.Matches(new BoundNotNode(yes), new Item()), Is.False);
    }

    [Test]
    public void Matches_PositiveCondition_AnyBindingSuffices()
    {
        Assert.That(_evaluator.Matches(Condition(QueryOperatorKind.Equals, false, true), new Item()), Is.True);
        Assert.That(_evaluator.Matches(Condition(QueryOperatorKind.Equals, false, false), new Item()), Is.False);
    }

    [Test]
    public void Matches_IsEmptyCondition_RequiresEveryBinding()
    {
        Assert.That(_evaluator.Matches(Condition(QueryOperatorKind.IsEmpty, true, true), new Item()), Is.True);
        Assert.That(_evaluator.Matches(Condition(QueryOperatorKind.IsEmpty, true, false), new Item()), Is.False);
    }

    [Test]
    public void Sort_OrdersByKeyWithNullsLast()
    {
        var a = new Item { DisplayName = "a" };
        var b = new Item { DisplayName = "b" };
        var empty = new Item { DisplayName = "" };
        var orderBy = new[]
        {
            new BoundOrderBy(i => i.DisplayName == "" ? null : i.DisplayName, Descending: false),
        };

        var sorted = _evaluator.Sort([empty, b, a], orderBy);

        Assert.That(sorted, Is.EqualTo(new[] { a, b, empty }));
    }

    [Test]
    public void Sort_Descending_ReversesOrderButKeepsNullsLast()
    {
        var a = new Item { DisplayName = "a" };
        var b = new Item { DisplayName = "b" };
        var empty = new Item { DisplayName = "" };
        var orderBy = new[]
        {
            new BoundOrderBy(i => i.DisplayName == "" ? null : i.DisplayName, Descending: true),
        };

        var sorted = _evaluator.Sort([empty, a, b], orderBy);

        Assert.That(sorted, Is.EqualTo(new[] { b, a, empty }));
    }

    [Test]
    public void Sort_MixedKeyTypes_FallBackToInvariantStringComparison()
    {
        var numeric = new Item { DisplayName = "numeric" };
        var text = new Item { DisplayName = "text" };
        var orderBy = new[]
        {
            new BoundOrderBy(i => i == numeric ? 20 : "abc", Descending: false),
        };

        Assert.That(_evaluator.Sort([text, numeric], orderBy), Is.EqualTo(new[] { numeric, text }));
    }

    [Test]
    public void Sort_SecondaryKey_BreaksTies()
    {
        var firstOld = new Item { DisplayName = "same", CreatedAt = new DateTime(2024, 1, 1) };
        var firstNew = new Item { DisplayName = "same", CreatedAt = new DateTime(2025, 1, 1) };
        var orderBy = new[]
        {
            new BoundOrderBy(i => i.DisplayName, Descending: false),
            new BoundOrderBy(i => i.CreatedAt, Descending: true),
        };

        var sorted = _evaluator.Sort([firstOld, firstNew], orderBy);

        Assert.That(sorted, Is.EqualTo(new[] { firstNew, firstOld }));
    }

    [Test]
    public void Sort_WithoutKeys_PreservesInputOrder()
    {
        var a = new Item();
        var b = new Item();

        Assert.That(_evaluator.Sort([b, a], []), Is.EqualTo(new[] { b, a }));
    }

    [Test]
    public void Sort_MixedNumericKeyTypes_CompareNumericallyNotAsText()
    {
        var integerTen = new Item { DisplayName = "ten" };
        var decimalNineAndAHalf = new Item { DisplayName = "nine-ish" };
        var orderBy = new[]
        {
            new BoundOrderBy(i => i == integerTen ? 10 : 9.5m, Descending: false),
        };

        Assert.That(_evaluator.Sort([integerTen, decimalNineAndAHalf], orderBy),
            Is.EqualTo(new[] { decimalNineAndAHalf, integerTen }),
            "as text \"10\" sorts before \"9.5\", numerically 9.5 must come first");
    }

    [Test]
    public void Sort_SecondaryKeyWithMissingValues_KeepsThoseRowsLastWithinTheTie()
    {
        var withSecond = new Item { DisplayName = "same", CreatedAt = new DateTime(2025, 1, 1) };
        var withoutSecond = new Item { DisplayName = "same" };
        var orderBy = new[]
        {
            new BoundOrderBy(i => i.DisplayName, Descending: false),
            new BoundOrderBy(i => i == withoutSecond ? null : i.CreatedAt, Descending: false),
        };

        Assert.That(_evaluator.Sort([withoutSecond, withSecond], orderBy),
            Is.EqualTo(new[] { withSecond, withoutSecond }));
    }
}
