using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class EmailFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_AcceptsAddress()
    {
        var ok = ((ITextImportable)new EmailFieldDefinition()).TryImportFromText("a@b.com", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((EmailFieldValue)v).Value, Is.EqualTo("a@b.com"));
    }

    [Test]
    public void TryImportFromText_RejectsNonAddress()
    {
        var ok = ((ITextImportable)new EmailFieldDefinition()).TryImportFromText("nope", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new EmailFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<EmailFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void TryCreateMatcher_Contains_MatchesAddressFragment()
    {
        var def = new EmailFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Contains, ["gmail"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new EmailFieldValue { FieldDefinitionId = def.Id, Value = "a@GMail.com" }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new EmailFieldValue { FieldDefinitionId = def.Id, Value = "a@web.de" }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new EmailFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Contains));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new EmailFieldValue { Value = "a@b.com" }), Is.EqualTo("a@b.com"));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
