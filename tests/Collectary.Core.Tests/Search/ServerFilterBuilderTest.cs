using System.Linq.Expressions;
using Collectary.Core.Domain;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Search;

[TestFixture]
public class ServerFilterBuilderTest
{
    private ServerFilterBuilder _builder = null!;

    [SetUp]
    public void SetUp() => _builder = new ServerFilterBuilder();

    private sealed class StubMatcher : IFieldConditionMatcher
    {
        public Expression<Func<Item, bool>>? Filter { get; init; }

        public Expression<Func<Item, bool>>? ServerFilter(IReadOnlyCollection<Guid> definitionIds) => Filter;

        public bool Matches(Item item, IReadOnlyCollection<Guid> definitionIds) => true;
    }

    private static BoundConditionNode Condition(params Expression<Func<Item, bool>>?[] filters) =>
        new()
        {
            Operator = QueryOperatorKind.Equals,
            Bindings = filters
                .Select(f => new BoundFieldMatch(new StubMatcher { Filter = f }, Array.Empty<Guid>()))
                .ToList(),
        };

    private static readonly Expression<Func<Item, bool>> NameIsA = item => item.DisplayName == "a";
    private static readonly Expression<Func<Item, bool>> NameIsB = item => item.DisplayName == "b";

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
        Assert.That(compiled(new Item { DisplayName = "a" }), Is.True);
        Assert.That(compiled(new Item { DisplayName = "b" }), Is.False);
    }

    [Test]
    public void Build_UntranslatableCondition_YieldsNoServerFilter()
    {
        Assert.That(_builder.Build(Condition((Expression<Func<Item, bool>>?)null)), Is.Null);
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

        Assert.That(compiled(new Item { DisplayName = "a" }), Is.True);
        Assert.That(compiled(new Item { DisplayName = "b" }), Is.True);
        Assert.That(compiled(new Item { DisplayName = "c" }), Is.False);
    }

    [Test]
    public void Build_AndWithUntranslatableSide_KeepsTheTranslatableSide()
    {
        var node = new BoundAndNode(Condition(NameIsA), Condition((Expression<Func<Item, bool>>?)null));

        var compiled = _builder.Build(node)!.Compile();

        Assert.That(compiled(new Item { DisplayName = "a" }), Is.True);
        Assert.That(compiled(new Item { DisplayName = "b" }), Is.False);
    }

    [Test]
    public void Build_OrWithUntranslatableSide_YieldsNoServerFilter()
    {
        var node = new BoundOrNode(Condition(NameIsA), Condition((Expression<Func<Item, bool>>?)null));

        Assert.That(_builder.Build(node), Is.Null);
    }

    [Test]
    public void Build_NotOverTranslatable_NegatesTheFilter()
    {
        var compiled = _builder.Build(new BoundNotNode(Condition(NameIsA)))!.Compile();

        Assert.That(compiled(new Item { DisplayName = "a" }), Is.False);
        Assert.That(compiled(new Item { DisplayName = "b" }), Is.True);
    }

    [Test]
    public void Build_NotOverUntranslatable_YieldsNoServerFilter()
    {
        var node = new BoundNotNode(Condition((Expression<Func<Item, bool>>?)null));

        Assert.That(_builder.Build(node), Is.Null);
    }

    [Test]
    public void Build_UntranslatableInsideNestedNegation_NeverShrinksTheResultSet()
    {
        var node = new BoundNotNode(new BoundAndNode(
            Condition(NameIsA),
            new BoundNotNode(Condition((Expression<Func<Item, bool>>?)null))));

        Assert.That(_builder.Build(node), Is.Null);
    }

    [Test]
    public void Build_AndOfTranslatables_CombinesBoth()
    {
        var node = new BoundAndNode(
            Condition(NameIsA),
            Condition((Expression<Func<Item, bool>>)(item => item.PresetId == Guid.Empty)));

        var compiled = _builder.Build(node)!.Compile();

        Assert.That(compiled(new Item { DisplayName = "a", PresetId = Guid.Empty }), Is.True);
        Assert.That(compiled(new Item { DisplayName = "a", PresetId = Guid.NewGuid() }), Is.False);
    }

    [Test]
    public void Build_AndWithNegatedUntranslatableSide_KeepsTheTranslatableSide()
    {
        var node = new BoundAndNode(
            Condition(NameIsA),
            new BoundNotNode(Condition((Expression<Func<Item, bool>>?)null)));

        var compiled = _builder.Build(node)!.Compile();

        Assert.That(compiled(new Item { DisplayName = "a" }), Is.True,
            "NOT over an unknown condition relaxes to true under AND, keeping the other side");
        Assert.That(compiled(new Item { DisplayName = "b" }), Is.False);
    }

    [Test]
    public void Build_OrWithNegatedUntranslatableSide_YieldsNoServerFilter()
    {
        var node = new BoundOrNode(
            new BoundNotNode(Condition((Expression<Func<Item, bool>>?)null)),
            Condition(NameIsA));

        Assert.That(_builder.Build(node), Is.Null,
            "OR with a side that may match anything cannot be narrowed on the server");
    }

    [Test]
    public void Build_NegatedAndWithUntranslatableSide_YieldsNoServerFilterEitherWay()
    {
        var untranslatable = (Expression<Func<Item, bool>>?)null;

        Assert.That(_builder.Build(new BoundNotNode(
            new BoundAndNode(Condition(untranslatable), Condition(NameIsA)))), Is.Null);
        Assert.That(_builder.Build(new BoundNotNode(
            new BoundAndNode(Condition(NameIsA), Condition(untranslatable)))), Is.Null);
    }

    [Test]
    public void Build_NegatedOrWithUntranslatableSide_KeepsTheNegatedTranslatableSide()
    {
        var untranslatable = (Expression<Func<Item, bool>>?)null;

        foreach (var node in new[]
        {
            new BoundNotNode(new BoundOrNode(Condition(untranslatable), Condition(NameIsA))),
            new BoundNotNode(new BoundOrNode(Condition(NameIsA), Condition(untranslatable))),
        })
        {
            var compiled = _builder.Build(node)!.Compile();
            Assert.That(compiled(new Item { DisplayName = "a" }), Is.False,
                "items that surely match the OR can be excluded");
            Assert.That(compiled(new Item { DisplayName = "b" }), Is.True);
        }
    }

    [Test]
    public void Build_IsEmptyConditionWithSeveralPushableBindings_IsNeverPushed()
    {
        var node = new BoundConditionNode
        {
            Operator = QueryOperatorKind.IsEmpty,
            Bindings =
            [
                new BoundFieldMatch(new StubMatcher { Filter = NameIsA }, Array.Empty<Guid>()),
                new BoundFieldMatch(new StubMatcher { Filter = NameIsB }, Array.Empty<Guid>()),
            ],
        };

        Assert.That(_builder.Build(node), Is.Null,
            "emptiness over several fields needs ALL of them empty, which OR-combined filters cannot express");
    }
}
