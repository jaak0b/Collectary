using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class DateFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new DateFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<DateFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void TryImportFromText_HonoursDateFormatPerCulture()
    {
        var de = ((ITextImportable)new DateFieldDefinition()).TryImportFromText("31.12.2024", new CultureInfo("de-DE"), out var v);
        Assert.That(de, Is.True);
        Assert.That(((DateFieldValue)v).Value, Is.EqualTo(new DateTime(2024, 12, 31)));

        var en = ((ITextImportable)new DateFieldDefinition()).TryImportFromText("12/31/2024", new CultureInfo("en-US"), out var v2);
        Assert.That(en, Is.True);
        Assert.That(((DateFieldValue)v2).Value, Is.EqualTo(new DateTime(2024, 12, 31)));
    }

    [Test]
    public void TryImportFromText_RejectsNonDate()
    {
        var ok = ((ITextImportable)new DateFieldDefinition()).TryImportFromText("not a date", new CultureInfo("en-US"), out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryCreateMatcher_Less_ComparesIsoDates()
    {
        var def = new DateFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Less, ["2025-01-01"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new DateFieldValue { FieldDefinitionId = def.Id, Value = new DateTime(2024, 12, 31) }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new DateFieldValue { FieldDefinitionId = def.Id, Value = new DateTime(2025, 1, 1) }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void TryCreateMatcher_UnparsableDate_ReportsInvalidValue()
    {
        ISearchableFieldDefinition search = new DateFieldDefinition();
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["nonsense"], out _, out var error), Is.False);
        Assert.That(error, Is.EqualTo(QueryErrorCode.InvalidValue));
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new DateFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Less));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new DateFieldValue { Value = new DateTime(2025, 1, 1) }),
            Is.EqualTo(new DateTime(2025, 1, 1)));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
