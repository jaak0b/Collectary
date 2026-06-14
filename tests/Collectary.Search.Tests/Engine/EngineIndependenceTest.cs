using System.Linq.Expressions;

namespace Collectary.Search.Tests.Engine;

public sealed record FakeItem(string Name, int Price);

file sealed class PredicateMatcher : IFieldConditionMatcher<FakeItem>
{
    private readonly Func<FakeItem, bool> _memory;
    private readonly Expression<Func<FakeItem, bool>>? _server;

    public PredicateMatcher(Func<FakeItem, bool> memory, Expression<Func<FakeItem, bool>>? server)
    {
        _memory = memory;
        _server = server;
    }

    public Expression<Func<FakeItem, bool>>? ServerFilter(IReadOnlyCollection<Guid> definitionIds) => _server;
    public bool Matches(FakeItem item, IReadOnlyCollection<Guid> definitionIds) => _memory(item);
}

file sealed class PriceField : ISearchField<FakeItem>
{
    public Func<FakeItem, IComparable?>? SortKey => item => item.Price;

    public bool TryBind(QueryOperatorKind op, IReadOnlyList<string> operands,
        out BoundFieldMatch<FakeItem>? match, out QueryErrorCode? error, out QueryErrorCode? notice)
    {
        match = null;
        error = null;
        notice = null;
        if (!int.TryParse(operands[0], out var value))
        {
            error = QueryErrorCode.InvalidValue;
            return false;
        }
        var matcher = op switch
        {
            QueryOperatorKind.Equals => new PredicateMatcher(i => i.Price == value, i => i.Price == value),
            QueryOperatorKind.Greater => new PredicateMatcher(i => i.Price > value, i => i.Price > value),
            _ => null,
        };
        if (matcher is null)
        {
            error = QueryErrorCode.OperatorNotSupported;
            return false;
        }
        match = new BoundFieldMatch<FakeItem>(matcher, []);
        return true;
    }
}

file sealed class NameField : ISearchField<FakeItem>
{
    public Func<FakeItem, IComparable?>? SortKey => item => item.Name;

    public bool TryBind(QueryOperatorKind op, IReadOnlyList<string> operands,
        out BoundFieldMatch<FakeItem>? match, out QueryErrorCode? error, out QueryErrorCode? notice)
    {
        error = null;
        notice = null;
        var needle = operands[0];
        var matcher = new PredicateMatcher(
            i => i.Name.Contains(needle, StringComparison.OrdinalIgnoreCase),
            i => i.Name.Contains(needle));
        match = new BoundFieldMatch<FakeItem>(matcher, []);
        return true;
    }
}

file sealed class FakeCatalog : ISearchCatalog<FakeItem>
{
    public bool IsKnownLabel(string label) =>
        label is "name" or "price";

    public IReadOnlyList<ISearchField<FakeItem>> FieldsFor(string label) => label switch
    {
        "price" => [new PriceField()],
        "name" => [new NameField()],
        _ => [],
    };
}

[TestFixture]
public class EngineIndependenceTest
{
    private static readonly IReadOnlyList<FakeItem> Items =
    [
        new("Red Caboose", 30),
        new("Blue Engine", 80),
        new("Green Wagon", 50),
    ];

    private static BoundQuery<FakeItem> Bind(string text)
    {
        var parsed = new QueryParser(new QueryLexer()).Parse(text);
        var result = new QueryBinder<FakeItem>(new FakeCatalog()).Bind(parsed.Query!);
        Assert.That(result.Errors, Is.Empty, "expected a clean bind");
        return result.Query!;
    }

    private static IReadOnlyList<FakeItem> RunInMemory(string text)
    {
        var bound = Bind(text);
        var evaluator = new QueryEvaluator<FakeItem>();
        var matched = Items.Where(i => evaluator.Matches(bound.Root, i));
        return evaluator.Sort(matched, bound.OrderBy);
    }

    [Test]
    public void Engine_FiltersAndSorts_OverAFakeItemModel()
    {
        var result = RunInMemory("price > 40 ORDER BY price DESC");
        Assert.That(result.Select(i => i.Name), Is.EqualTo(new[] { "Blue Engine", "Green Wagon" }));
    }

    [Test]
    public void Engine_CombinesConditions_WithAnd()
    {
        var result = RunInMemory("name ~ e AND price > 40");
        Assert.That(result.Select(i => i.Name), Is.EquivalentTo(new[] { "Blue Engine", "Green Wagon" }));
    }

    [Test]
    public void Engine_BuildsAServerExpression_ThatFiltersTheSameWay()
    {
        var bound = Bind("price > 40");
        var server = new ServerFilterBuilder<FakeItem>().Build(bound.Root);
        Assert.That(server, Is.Not.Null);
        var matched = Items.AsQueryable().Where(server!).Select(i => i.Name).ToList();
        Assert.That(matched, Is.EquivalentTo(new[] { "Blue Engine", "Green Wagon" }));
    }

    [Test]
    public void Engine_ReportsUnknownField()
    {
        var parsed = new QueryParser(new QueryLexer()).Parse("colour = red");
        var result = new QueryBinder<FakeItem>(new FakeCatalog()).Bind(parsed.Query!);
        Assert.That(result.Query, Is.Null);
        Assert.That(result.Errors.Select(e => e.Code), Does.Contain(QueryErrorCode.UnknownField));
    }
}
