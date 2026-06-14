using System.Linq.Expressions;

namespace Collectary.Search.Tests.Engine;

public abstract record BagValue(Guid FieldDefinitionId)
{
    public abstract bool IsEmpty { get; }
}

public sealed record NumberValue(Guid FieldDefinitionId, int? Number) : BagValue(FieldDefinitionId)
{
    public override bool IsEmpty => Number is null;
}

public sealed record LabelValue(Guid FieldDefinitionId, string? Text) : BagValue(FieldDefinitionId)
{
    public override bool IsEmpty => string.IsNullOrEmpty(Text);
}

public sealed record Widget(IReadOnlyList<BagValue> Values);

[TestFixture]
public class ValueBagSearchTest
{
    private static readonly Guid PriceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid NameId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ItemValueModel<Widget, BagValue> Model() => new(
        values: w => w.Values,
        definitionId: v => v.FieldDefinitionId,
        isEmpty: v => v.IsEmpty,
        valuesExpression: w => w.Values,
        definitionIdExpression: v => v.FieldDefinitionId);

    private static ComparableFieldSearch<Widget, BagValue, NumberValue, int> Price() => new(
        Model(),
        v => v.Number,
        v => v.Number,
        raw => int.TryParse(raw, out var n) ? n : null);

    private static StringFieldSearch<Widget, BagValue, LabelValue> Name() => new(
        Model(), v => v.Text, v => v.Text);

    private static Widget Make(int price, string name) =>
        new([new NumberValue(PriceId, price), new LabelValue(NameId, name)]);

    private static readonly IReadOnlyList<Widget> Items =
    [
        Make(30, "Red Caboose"),
        Make(80, "Blue Engine"),
        Make(50, "Green Wagon"),
    ];

    [Test]
    public void Comparable_MemoryPredicate_FiltersByOperator()
    {
        Assert.That(Price().TryCreateMatcher(QueryOperatorKind.Greater, ["40"], out var matcher, out _), Is.True);
        var hits = Items.Where(w => matcher!.Matches(w, [PriceId])).Count();
        Assert.That(hits, Is.EqualTo(2));
    }

    [Test]
    public void Comparable_ServerFilter_TranslatesAndFiltersTheSameWay()
    {
        Price().TryCreateMatcher(QueryOperatorKind.Greater, ["40"], out var matcher, out _);
        var server = matcher!.ServerFilter([PriceId]);
        Assert.That(server, Is.Not.Null);
        var hits = Items.AsQueryable().Where(server!).Count();
        Assert.That(hits, Is.EqualTo(2));
    }

    [Test]
    public void Comparable_RespectsDefinitionIds()
    {
        Price().TryCreateMatcher(QueryOperatorKind.Greater, ["40"], out var matcher, out _);
        Assert.That(Items.Where(w => matcher!.Matches(w, [NameId])), Is.Empty);
    }

    [Test]
    public void String_Contains_IsCaseInsensitive()
    {
        Assert.That(Name().TryCreateMatcher(QueryOperatorKind.Contains, ["engine"], out var matcher, out _), Is.True);
        var hits = Items.Where(w => matcher!.Matches(w, [NameId])).Select(w => w.Values.OfType<LabelValue>().First().Text);
        Assert.That(hits, Is.EqualTo(new[] { "Blue Engine" }));
    }

    [Test]
    public void Emptiness_IsEmpty_MatchesMissingValues()
    {
        Price().TryCreateMatcher(QueryOperatorKind.IsEmpty, [], out var matcher, out _);
        var blank = new Widget([new NumberValue(PriceId, null)]);
        Assert.That(matcher!.Matches(blank, [PriceId]), Is.True);
        Assert.That(matcher.Matches(Make(30, "x"), [PriceId]), Is.False);
    }

    [Test]
    public void SortKey_ReadsTheTypedValue()
    {
        var key = Price().SortKey(Items[1], new NumberValue(PriceId, 80));
        Assert.That(key, Is.EqualTo(80));
    }
}
