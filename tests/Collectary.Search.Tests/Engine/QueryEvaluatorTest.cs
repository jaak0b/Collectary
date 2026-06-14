using System.Linq.Expressions;

namespace Collectary.Search.Tests.Engine;

[TestFixture]
public class QueryEvaluatorTest
{
    private QueryEvaluator<FakeItem> _evaluator = null!;

    [SetUp]
    public void SetUp() => _evaluator = new QueryEvaluator<FakeItem>();

    private sealed class FixedMatcher : IFieldConditionMatcher<FakeItem>
    {
        private readonly Func<FakeItem, bool> _predicate;

        public FixedMatcher(bool result) : this(_ => result) { }

        public FixedMatcher(Func<FakeItem, bool> predicate) => _predicate = predicate;

        public Expression<Func<FakeItem, bool>>? ServerFilter(IReadOnlyCollection<Guid> definitionIds) => null;

        public bool Matches(FakeItem item, IReadOnlyCollection<Guid> definitionIds) => _predicate(item);
    }

    private static BoundConditionNode<FakeItem> Condition(QueryOperatorKind op, params bool[] bindingResults) =>
        new()
        {
            Operator = op,
            Bindings = bindingResults
                .Select(r => new BoundFieldMatch<FakeItem>(new FixedMatcher(r), Array.Empty<Guid>()))
                .ToList(),
        };

    private static FakeItem Item(string name = "", int price = 0) => new(name, price);

    [Test]
    public void Matches_NullRoot_MatchesEverything()
    {
        Assert.That(_evaluator.Matches(null, Item()), Is.True);
    }

    [Test]
    public void Matches_BooleanComposition_EvaluatesAndOrNot()
    {
        var yes = Condition(QueryOperatorKind.Equals, true);
        var no = Condition(QueryOperatorKind.Equals, false);

        Assert.That(_evaluator.Matches(new BoundAndNode<FakeItem>(yes, yes), Item()), Is.True);
        Assert.That(_evaluator.Matches(new BoundAndNode<FakeItem>(yes, no), Item()), Is.False);
        Assert.That(_evaluator.Matches(new BoundOrNode<FakeItem>(no, yes), Item()), Is.True);
        Assert.That(_evaluator.Matches(new BoundOrNode<FakeItem>(no, no), Item()), Is.False);
        Assert.That(_evaluator.Matches(new BoundNotNode<FakeItem>(no), Item()), Is.True);
        Assert.That(_evaluator.Matches(new BoundNotNode<FakeItem>(yes), Item()), Is.False);
    }

    [Test]
    public void Matches_PositiveCondition_AnyBindingSuffices()
    {
        Assert.That(_evaluator.Matches(Condition(QueryOperatorKind.Equals, false, true), Item()), Is.True);
        Assert.That(_evaluator.Matches(Condition(QueryOperatorKind.Equals, false, false), Item()), Is.False);
    }

    [Test]
    public void Matches_IsEmptyCondition_RequiresEveryBinding()
    {
        Assert.That(_evaluator.Matches(Condition(QueryOperatorKind.IsEmpty, true, true), Item()), Is.True);
        Assert.That(_evaluator.Matches(Condition(QueryOperatorKind.IsEmpty, true, false), Item()), Is.False);
    }

    [Test]
    public void Sort_OrdersByKeyWithNullsLast()
    {
        var a = Item("a");
        var b = Item("b");
        var empty = Item("");
        var orderBy = new[]
        {
            new BoundOrderBy<FakeItem>(i => i.Name == "" ? null : i.Name, Descending: false),
        };

        var sorted = _evaluator.Sort([empty, b, a], orderBy);

        Assert.That(sorted, Is.EqualTo(new[] { a, b, empty }));
    }

    [Test]
    public void Sort_Descending_ReversesOrderButKeepsNullsLast()
    {
        var a = Item("a");
        var b = Item("b");
        var empty = Item("");
        var orderBy = new[]
        {
            new BoundOrderBy<FakeItem>(i => i.Name == "" ? null : i.Name, Descending: true),
        };

        var sorted = _evaluator.Sort([empty, a, b], orderBy);

        Assert.That(sorted, Is.EqualTo(new[] { b, a, empty }));
    }

    [Test]
    public void Sort_MixedKeyTypes_FallBackToInvariantStringComparison()
    {
        var numeric = Item("numeric");
        var text = Item("text");
        var orderBy = new[]
        {
            new BoundOrderBy<FakeItem>(i => i == numeric ? 20 : "abc", Descending: false),
        };

        Assert.That(_evaluator.Sort([text, numeric], orderBy), Is.EqualTo(new[] { numeric, text }));
    }

    [Test]
    public void Sort_SecondaryKey_BreaksTies()
    {
        var firstOld = Item("same", 1);
        var firstNew = Item("same", 2);
        var orderBy = new[]
        {
            new BoundOrderBy<FakeItem>(i => i.Name, Descending: false),
            new BoundOrderBy<FakeItem>(i => i.Price, Descending: true),
        };

        var sorted = _evaluator.Sort([firstOld, firstNew], orderBy);

        Assert.That(sorted, Is.EqualTo(new[] { firstNew, firstOld }));
    }

    [Test]
    public void Sort_WithoutKeys_PreservesInputOrder()
    {
        var a = Item("x");
        var b = Item("y");

        Assert.That(_evaluator.Sort([b, a], []), Is.EqualTo(new[] { b, a }));
    }

    [Test]
    public void Sort_MixedNumericKeyTypes_CompareNumericallyNotAsText()
    {
        var integerTen = Item("ten");
        var decimalNineAndAHalf = Item("nine-ish");
        var orderBy = new[]
        {
            new BoundOrderBy<FakeItem>(i => i == integerTen ? 10 : 9.5m, Descending: false),
        };

        Assert.That(_evaluator.Sort([integerTen, decimalNineAndAHalf], orderBy),
            Is.EqualTo(new[] { decimalNineAndAHalf, integerTen }),
            "as text \"10\" sorts before \"9.5\", numerically 9.5 must come first");
    }

    [Test]
    public void Sort_SecondaryKeyWithMissingValues_KeepsThoseRowsLastWithinTheTie()
    {
        var withSecond = Item("same", 2);
        var withoutSecond = Item("same", 0);
        var orderBy = new[]
        {
            new BoundOrderBy<FakeItem>(i => i.Name, Descending: false),
            new BoundOrderBy<FakeItem>(i => i == withoutSecond ? null : i.Price, Descending: false),
        };

        Assert.That(_evaluator.Sort([withoutSecond, withSecond], orderBy),
            Is.EqualTo(new[] { withSecond, withoutSecond }));
    }
}
