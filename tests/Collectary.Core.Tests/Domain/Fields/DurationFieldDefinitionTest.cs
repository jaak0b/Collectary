using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class DurationFieldDefinitionTest
{
    [TestCase("90", 90)]
    [TestCase("1:30", 90)]
    [TestCase("1:30:00", 90)]
    [TestCase("1:30:45", 90)]
    [TestCase("2 h 15 min", 135)]
    [TestCase("1h30m", 90)]
    public void TryImportFromText_ParsesDurations(string raw, int expectedMinutes)
    {
        var ok = ((ITextImportable)new DurationFieldDefinition()).TryImportFromText(raw, CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((DurationFieldValue)v).TotalMinutes, Is.EqualTo(expectedMinutes));
    }

    [TestCase("soon")]
    [TestCase("-5")]
    [TestCase("-1:30")]
    [TestCase("1:75")]
    [TestCase("5h 99999999999m")]
    [TestCase("99999999999:00")]
    public void TryImportFromText_RejectsInvalidDurations(string raw)
    {
        var ok = ((ITextImportable)new DurationFieldDefinition()).TryImportFromText(raw, CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new DurationFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<DurationFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void TryCreateMatcher_Greater_ParsesDurationNotation()
    {
        var def = new DurationFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Greater, ["1:00"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new DurationFieldValue { FieldDefinitionId = def.Id, TotalMinutes = 90 }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new DurationFieldValue { FieldDefinitionId = def.Id, TotalMinutes = 45 }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void TryCreateMatcher_Equals_AcceptsHourMinuteWords()
    {
        var def = new DurationFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["1h 30m"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new DurationFieldValue { FieldDefinitionId = def.Id, TotalMinutes = 90 }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new DurationFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.GreaterOrEqual));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new DurationFieldValue { TotalMinutes = 90 }), Is.EqualTo(90));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
