using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class CountryFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_UppercasesTwoLetterCode()
    {
        var ok = ((ITextImportable)new CountryFieldDefinition()).TryImportFromText("de", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((CountryFieldValue)v).Code, Is.EqualTo("DE"));
    }

    [Test]
    public void TryImportFromText_RejectsNonCode()
    {
        var ok = ((ITextImportable)new CountryFieldDefinition()).TryImportFromText("Germany", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryImportFromText_RejectsUnknownTwoLetterCode()
    {
        var ok = ((ITextImportable)new CountryFieldDefinition()).TryImportFromText("xx", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new CountryFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<CountryFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new CountryFieldDefinition(), Is.InstanceOf<IListDisplayable>());

    [Test]
    public void TryCreateMatcher_Equals_MatchesIsoCodeCaseInsensitively()
    {
        var def = new CountryFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["de"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new CountryFieldValue { FieldDefinitionId = def.Id, Code = "DE" }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new CountryFieldValue { FieldDefinitionId = def.Id, Code = "US" }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new CountryFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Equals));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new CountryFieldValue { Code = "DE" }), Is.EqualTo("DE"));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
