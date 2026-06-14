using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class QrCodeFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_StoresContent()
    {
        var ok = ((ITextImportable)new QrCodeFieldDefinition()).TryImportFromText("shelf-A1", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((QrCodeFieldValue)v).Content, Is.EqualTo("shelf-A1"));
    }

    [Test]
    public void TryImportFromText_RejectsWhitespace()
    {
        var ok = ((ITextImportable)new QrCodeFieldDefinition()).TryImportFromText("  ", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new QrCodeFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<QrCodeFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new QrCodeFieldDefinition(), Is.InstanceOf<IListDisplayable>());

    [Test]
    public void TryCreateMatcher_Contains_MatchesContentFragment()
    {
        var def = new QrCodeFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Contains, ["shelf"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new QrCodeFieldValue { FieldDefinitionId = def.Id, Content = "Shelf-A1" }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new QrCodeFieldValue { FieldDefinitionId = def.Id, Content = "box-9" }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new QrCodeFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Equals));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new QrCodeFieldValue { Content = "A1" }), Is.EqualTo("A1"));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
