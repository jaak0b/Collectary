using System.Linq.Expressions;

namespace Collectary.Search.Tests.Engine;

[TestFixture]
public class ServerFilterBuilderTest
{
    private ServerFilterBuilder<FakeItem> _builder = null!;

    [SetUp]
    public void SetUp() => _builder = new ServerFilterBuilder<FakeItem>();

    private sealed class StubMatcher : IFieldConditionMatcher<FakeItem>
    {
        public Expression<Func<FakeItem, bool>>? Filter { get; init; }

        public Expression<Func<FakeItem, bool>>? ServerFilter(IReadOnlyCollection<Guid> definitionIds) => Filter;

        public bool Matches(FakeItem item, IReadOnlyCollection<Guid> definitionIds) => true;
    }

    private static BoundConditionNode<FakeItem> Condition(params Expression<Func<FakeItem, bool>>?[] filters) =>
        new()
        {
            Operator = QueryOperatorKind.Equals,
            Bindings = filters
                .Select(f => new BoundFieldMatch<FakeItem>(new StubMatcher { Filter = f }, Array.Empty<Guid>()))
                .ToList(),
        };

    private static readonly Expression<Func<FakeItem, bool>> NameIsA = item => item.Name == "a";
    private static readonly Expression<Func<FakeItem, bool>> NameIsB = item => item.Name == "b";

    private static FakeItem Named(string name) => new(name, 0);

    [Test]
    public void Build_NullRoot_ReturnsNull()
    {
        Assert.That(_builder.Build(null), Is.Null);
    }

    [Test]
    public void Build_TranslatableCondition_ReturnsItsFilter()
    {
        var filter = _builder.Build(Condition(NameIsA));

        Assert.That(filter, Is.Not.Null);
        var compiled = filter!.Compile();
        Assert.That(compiled(Named("a")), Is.True);
        Assert.That(compiled(Named("b")), Is.False);
    }

    [Test]
    public void Build_UntranslatableCondition_YieldsNoServerFilter()
    {
        Assert.That(_builder.Build(Condition((Expression<Func<FakeItem, bool>>?)null)), Is.Null);
    }

    [Test]
    public void Build_ConditionWithMixedBindings_YieldsNoServerFilter()
    {
        Assert.That(_builder.Build(Condition(NameIsA, null)), Is.Null);
    }

    [Test]
    public void Build_MultipleTranslatableBindings_CombinesWithOr()
    {
        var compiled = _builder.Build(Condition(NameIsA, NameIsB))!.Compile();

        Assert.That(compiled(Named("a")), Is.True);
        Assert.That(compiled(Named("b")), Is.True);
        Assert.That(compiled(Named("c")), Is.False);
    }

    [Test]
    public void Build_AndWithUntranslatableSide_KeepsTheTranslatableSide()
    {
        var node = new BoundAndNode<FakeItem>(Condition(NameIsA), Condition((Expression<Func<FakeItem, bool>>?)null));

        var compiled = _builder.Build(node)!.Compile();

        Assert.That(compiled(Named("a")), Is.True);
        Assert.That(compiled(Named("b")), Is.False);
    }

    [Test]
    public void Build_OrWithUntranslatableSide_YieldsNoServerFilter()
    {
        var node = new BoundOrNode<FakeItem>(Condition(NameIsA), Condition((Expression<Func<FakeItem, bool>>?)null));

        Assert.That(_builder.Build(node), Is.Null);
    }

    [Test]
    public void Build_NotOverTranslatable_NegatesTheFilter()
    {
        var compiled = _builder.Build(new BoundNotNode<FakeItem>(Condition(NameIsA)))!.Compile();

        Assert.That(compiled(Named("a")), Is.False);
        Assert.That(compiled(Named("b")), Is.True);
    }

    [Test]
    public void Build_NotOverUntranslatable_YieldsNoServerFilter()
    {
        var node = new BoundNotNode<FakeItem>(Condition((Expression<Func<FakeItem, bool>>?)null));

        Assert.That(_builder.Build(node), Is.Null);
    }

    [Test]
    public void Build_UntranslatableInsideNestedNegation_NeverShrinksTheResultSet()
    {
        var node = new BoundNotNode<FakeItem>(new BoundAndNode<FakeItem>(
            Condition(NameIsA),
            new BoundNotNode<FakeItem>(Condition((Expression<Func<FakeItem, bool>>?)null))));

        Assert.That(_builder.Build(node), Is.Null);
    }

    [Test]
    public void Build_AndOfTranslatables_CombinesBoth()
    {
        var node = new BoundAndNode<FakeItem>(
            Condition(NameIsA),
            Condition((Expression<Func<FakeItem, bool>>)(item => item.Price == 0)));

        var compiled = _builder.Build(node)!.Compile();

        Assert.That(compiled(new FakeItem("a", 0)), Is.True);
        Assert.That(compiled(new FakeItem("a", 7)), Is.False);
    }

    [Test]
    public void Build_AndWithNegatedUntranslatableSide_KeepsTheTranslatableSide()
    {
        var node = new BoundAndNode<FakeItem>(
            Condition(NameIsA),
            new BoundNotNode<FakeItem>(Condition((Expression<Func<FakeItem, bool>>?)null)));

        var compiled = _builder.Build(node)!.Compile();

        Assert.That(compiled(Named("a")), Is.True,
            "NOT over an unknown condition relaxes to true under AND, keeping the other side");
        Assert.That(compiled(Named("b")), Is.False);
    }

    [Test]
    public void Build_OrWithNegatedUntranslatableSide_YieldsNoServerFilter()
    {
        var node = new BoundOrNode<FakeItem>(
            new BoundNotNode<FakeItem>(Condition((Expression<Func<FakeItem, bool>>?)null)),
            Condition(NameIsA));

        Assert.That(_builder.Build(node), Is.Null,
            "OR with a side that may match anything cannot be narrowed on the server");
    }

    [Test]
    public void Build_NegatedAndWithUntranslatableSide_YieldsNoServerFilterEitherWay()
    {
        var untranslatable = (Expression<Func<FakeItem, bool>>?)null;

        Assert.That(_builder.Build(new BoundNotNode<FakeItem>(
            new BoundAndNode<FakeItem>(Condition(untranslatable), Condition(NameIsA)))), Is.Null);
        Assert.That(_builder.Build(new BoundNotNode<FakeItem>(
            new BoundAndNode<FakeItem>(Condition(NameIsA), Condition(untranslatable)))), Is.Null);
    }

    [Test]
    public void Build_NegatedOrWithUntranslatableSide_KeepsTheNegatedTranslatableSide()
    {
        var untranslatable = (Expression<Func<FakeItem, bool>>?)null;

        foreach (var node in new[]
        {
            new BoundNotNode<FakeItem>(new BoundOrNode<FakeItem>(Condition(untranslatable), Condition(NameIsA))),
            new BoundNotNode<FakeItem>(new BoundOrNode<FakeItem>(Condition(NameIsA), Condition(untranslatable))),
        })
        {
            var compiled = _builder.Build(node)!.Compile();
            Assert.That(compiled(Named("a")), Is.False,
                "items that surely match the OR can be excluded");
            Assert.That(compiled(Named("b")), Is.True);
        }
    }

    [Test]
    public void Build_IsEmptyConditionWithSeveralPushableBindings_IsNeverPushed()
    {
        var node = new BoundConditionNode<FakeItem>
        {
            Operator = QueryOperatorKind.IsEmpty,
            Bindings =
            [
                new BoundFieldMatch<FakeItem>(new StubMatcher { Filter = NameIsA }, Array.Empty<Guid>()),
                new BoundFieldMatch<FakeItem>(new StubMatcher { Filter = NameIsB }, Array.Empty<Guid>()),
            ],
        };

        Assert.That(_builder.Build(node), Is.Null,
            "emptiness over several fields needs ALL of them empty, which OR-combined filters cannot express");
    }
}
