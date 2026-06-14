using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class TagsFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_SplitsTags()
    {
        var ok = ((ITextImportable)new TagsFieldDefinition()).TryImportFromText("x, y; z", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((TagsFieldValue)v).Tags, Is.EqualTo(new[] { "x", "y", "z" }));
    }

    [Test]
    public void TryImportFromText_RejectsWhitespace()
    {
        var ok = ((ITextImportable)new TagsFieldDefinition()).TryImportFromText("  ", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new TagsFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<TagsFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void TryCreateMatcher_Contains_MatchesAnyTagFragment()
    {
        var def = new TagsFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Contains, ["rar"], out var matcher, out _), Is.True);

        var item = new Item
        {
            Values = [new TagsFieldValue { FieldDefinitionId = def.Id, Tags = ["mint", "Rare"] }],
        };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        ((TagsFieldValue)item.Values[0]).Tags = ["mint"];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new TagsFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Contains));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new TagsFieldValue { Tags = ["b", "a"] }), Is.EqualTo("b, a"));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
