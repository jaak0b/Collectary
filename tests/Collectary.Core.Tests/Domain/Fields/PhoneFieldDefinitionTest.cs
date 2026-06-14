using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class PhoneFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_AcceptsPhoneNumber()
    {
        var ok = ((ITextImportable)new PhoneFieldDefinition()).TryImportFromText("+49 170 1234567", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((PhoneFieldValue)v).Value, Is.EqualTo("+49 170 1234567"));
    }

    [Test]
    public void TryImportFromText_RejectsLetters()
    {
        var ok = ((ITextImportable)new PhoneFieldDefinition()).TryImportFromText("call me", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new PhoneFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<PhoneFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void TryCreateMatcher_Contains_MatchesNumberFragment()
    {
        var def = new PhoneFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Contains, ["170"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new PhoneFieldValue { FieldDefinitionId = def.Id, Value = "+49 170 1234567" }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new PhoneFieldValue { FieldDefinitionId = def.Id, Value = "+49 30 999" }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new PhoneFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Contains));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new PhoneFieldValue { Value = "+49" }), Is.EqualTo("+49"));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
