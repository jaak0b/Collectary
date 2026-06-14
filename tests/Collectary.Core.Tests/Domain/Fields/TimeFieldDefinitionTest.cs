using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class TimeFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_AcceptsTimeOfDay()
    {
        var ok = ((ITextImportable)new TimeFieldDefinition()).TryImportFromText("14:30", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((TimeFieldValue)v).Value, Is.EqualTo("14:30"));
    }

    [Test]
    public void TryImportFromText_RejectsNonTime()
    {
        var ok = ((ITextImportable)new TimeFieldDefinition()).TryImportFromText("hello", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new TimeFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<TimeFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void TryCreateMatcher_Equals_MatchesStoredTime()
    {
        var def = new TimeFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["14:30"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new TimeFieldValue { FieldDefinitionId = def.Id, Value = "14:30" }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new TimeFieldValue { FieldDefinitionId = def.Id, Value = "09:00" }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new TimeFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Equals));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new TimeFieldValue { Value = "14:30" }), Is.EqualTo("14:30"));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
