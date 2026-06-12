using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Search;

[TestFixture]
public class StringListFieldSearchTest
{
    private static readonly Guid DefinitionId = Guid.NewGuid();

    private StringListFieldSearch<TagsFieldValue> _search = null!;

    [SetUp]
    public void SetUp() => _search = new StringListFieldSearch<TagsFieldValue>(v => v.Tags);

    private static Item ItemWithTags(params string[] tags) => new()
    {
        Values = [new TagsFieldValue { FieldDefinitionId = DefinitionId, Tags = tags.ToList() }],
    };

    private IFieldConditionMatcher Matcher(QueryOperatorKind op, params string[] operands)
    {
        Assert.That(_search.TryCreateMatcher(op, operands, out var matcher, out _), Is.True);
        return matcher!;
    }

    [Test]
    public void Operators_CoverEntryComparisonsAndEmptiness()
    {
        Assert.That(_search.Operators, Is.EquivalentTo(new[]
        {
            QueryOperatorKind.Equals, QueryOperatorKind.NotEquals,
            QueryOperatorKind.Contains, QueryOperatorKind.NotContains,
            QueryOperatorKind.In, QueryOperatorKind.IsEmpty, QueryOperatorKind.IsNotEmpty,
        }));
    }

    [Test]
    public void Equals_MatchesWhenAnyEntryEqualsTheOperand()
    {
        var matcher = Matcher(QueryOperatorKind.Equals, "rare");

        Assert.That(matcher.Matches(ItemWithTags("Mint", "RARE"), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithTags("mint"), [DefinitionId]), Is.False);
    }

    [Test]
    public void Contains_MatchesWhenAnyEntryContainsTheOperand()
    {
        var matcher = Matcher(QueryOperatorKind.Contains, "ar");

        Assert.That(matcher.Matches(ItemWithTags("rare"), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithTags("mint"), [DefinitionId]), Is.False);
    }

    [Test]
    public void NegatedOperators_RequireANonEmptyList()
    {
        Assert.That(Matcher(QueryOperatorKind.NotEquals, "rare")
            .Matches(ItemWithTags("mint"), [DefinitionId]), Is.True);
        Assert.That(Matcher(QueryOperatorKind.NotEquals, "rare")
            .Matches(ItemWithTags("rare", "mint"), [DefinitionId]), Is.False);
        Assert.That(Matcher(QueryOperatorKind.NotEquals, "rare")
            .Matches(new Item(), [DefinitionId]), Is.False);
        Assert.That(Matcher(QueryOperatorKind.NotEquals, "rare")
            .Matches(ItemWithTags(), [DefinitionId]), Is.False,
            "a present-but-empty list is still empty for !=");
        Assert.That(Matcher(QueryOperatorKind.NotContains, "ar")
            .Matches(ItemWithTags("mint"), [DefinitionId]), Is.True);
        Assert.That(Matcher(QueryOperatorKind.NotContains, "ar")
            .Matches(ItemWithTags("rare"), [DefinitionId]), Is.False);
        Assert.That(Matcher(QueryOperatorKind.NotContains, "ar")
            .Matches(ItemWithTags("rare", "mint"), [DefinitionId]), Is.False,
            "one matching entry among several still disqualifies !~");
        Assert.That(Matcher(QueryOperatorKind.NotContains, "ar")
            .Matches(ItemWithTags(), [DefinitionId]), Is.False);
    }

    [Test]
    public void In_MatchesWhenAnyEntryEqualsAnyOperand()
    {
        var matcher = Matcher(QueryOperatorKind.In, "rare", "mint");

        Assert.That(matcher.Matches(ItemWithTags("MINT"), [DefinitionId]), Is.True);
        Assert.That(matcher.Matches(ItemWithTags("damaged"), [DefinitionId]), Is.False);
    }

    [Test]
    public void IsEmpty_MatchesMissingValueOrEmptyList()
    {
        Assert.That(Matcher(QueryOperatorKind.IsEmpty).Matches(new Item(), [DefinitionId]), Is.True);
        Assert.That(Matcher(QueryOperatorKind.IsEmpty).Matches(ItemWithTags(), [DefinitionId]), Is.True);
        Assert.That(Matcher(QueryOperatorKind.IsEmpty).Matches(ItemWithTags("x"), [DefinitionId]), Is.False);
    }

    [Test]
    public void ServerFilter_IsNotAvailableForListValues()
    {
        Assert.That(Matcher(QueryOperatorKind.Equals, "rare").ServerFilter([DefinitionId]), Is.Null);
    }

    [Test]
    public void SortKey_JoinsEntries()
    {
        Assert.That(_search.SortKey(new Item(), new TagsFieldValue { Tags = ["b", "a"] }), Is.EqualTo("b, a"));
        Assert.That(_search.SortKey(new Item(), null), Is.Null);
    }

    [Test]
    public void UnsupportedOperator_ReportsOperatorNotSupported()
    {
        Assert.That(_search.TryCreateMatcher(QueryOperatorKind.Greater, ["1"], out _, out var error), Is.False);
        Assert.That(error, Is.EqualTo(QueryErrorCode.OperatorNotSupported));
    }
}
