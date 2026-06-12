using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class UrlFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_AcceptsAbsoluteUrl()
    {
        var ok = ((ITextImportable)new UrlFieldDefinition()).TryImportFromText("https://example.com", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((UrlFieldValue)v).Url, Is.EqualTo("https://example.com"));
    }

    [Test]
    public void TryImportFromText_PrependsSchemeToSchemelessWww()
    {
        var ok = ((ITextImportable)new UrlFieldDefinition()).TryImportFromText("www.example.com", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((UrlFieldValue)v).Url, Is.EqualTo("https://www.example.com"));
    }

    [Test]
    public void TryImportFromText_RejectsPlainText()
    {
        var ok = ((ITextImportable)new UrlFieldDefinition()).TryImportFromText("not a url", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new UrlFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<UrlFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void TryCreateMatcher_Contains_MatchesUrlFragment()
    {
        var def = new UrlFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Contains, ["example"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new UrlFieldValue { FieldDefinitionId = def.Id, Url = "https://Example.com" }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new UrlFieldValue { FieldDefinitionId = def.Id, Url = "https://other.org" }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new UrlFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Contains));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new UrlFieldValue { Url = "https://a.de" }), Is.EqualTo("https://a.de"));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
