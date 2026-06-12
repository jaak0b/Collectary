using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class RatingFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_ParsesStarsWithinRange()
    {
        var ok = ((ITextImportable)new RatingFieldDefinition { MaxStars = 5 }).TryImportFromText("4", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((RatingFieldValue)v).Stars, Is.EqualTo(4));
    }

    [Test]
    public void TryImportFromText_RejectsOutOfRange()
    {
        var ok = ((ITextImportable)new RatingFieldDefinition { MaxStars = 5 }).TryImportFromText("9", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryImportFromText_RejectsNonNumber()
    {
        var ok = ((ITextImportable)new RatingFieldDefinition()).TryImportFromText("great", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new RatingFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<RatingFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void DefaultsToFiveStars() =>
        Assert.That(new RatingFieldDefinition().MaxStars, Is.EqualTo(5));

    [Test]
    public void TryCreateMatcher_GreaterOrEqual_MatchesStars()
    {
        var def = new RatingFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.GreaterOrEqual, ["4"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new RatingFieldValue { FieldDefinitionId = def.Id, Stars = 4 }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new RatingFieldValue { FieldDefinitionId = def.Id, Stars = 3 }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new RatingFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Greater));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new RatingFieldValue { Stars = 4 }), Is.EqualTo(4));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
