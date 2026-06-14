using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class BarcodeFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_StoresCode()
    {
        var ok = ((ITextImportable)new BarcodeFieldDefinition()).TryImportFromText("4006381333931", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((BarcodeFieldValue)v).Code, Is.EqualTo("4006381333931"));
    }

    [Test]
    public void TryImportFromText_RejectsWhitespace()
    {
        var ok = ((ITextImportable)new BarcodeFieldDefinition()).TryImportFromText("  ", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new BarcodeFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<BarcodeFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new BarcodeFieldDefinition(), Is.InstanceOf<IListDisplayable>());

    [Test]
    public void TryCreateMatcher_Equals_MatchesStoredCode()
    {
        var def = new BarcodeFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["4006381333931"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new BarcodeFieldValue { FieldDefinitionId = def.Id, Code = "4006381333931" }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new BarcodeFieldValue { FieldDefinitionId = def.Id, Code = "1111111111111" }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new BarcodeFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Contains));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new BarcodeFieldValue { Code = "42" }), Is.EqualTo("42"));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
