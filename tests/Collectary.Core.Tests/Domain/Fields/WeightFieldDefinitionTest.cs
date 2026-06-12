using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class WeightFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_ParsesAmountAndUnit()
    {
        var ok = ((ITextImportable)new WeightFieldDefinition()).TryImportFromText("250 g", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        var w = (WeightFieldValue)v;
        Assert.That(w.Amount, Is.EqualTo(250m));
        Assert.That(w.Unit, Is.EqualTo("g"));
    }

    [Test]
    public void TryImportFromText_RejectsGibberish()
    {
        var ok = ((ITextImportable)new WeightFieldDefinition()).TryImportFromText("heavy", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new WeightFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<WeightFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new WeightFieldDefinition(), Is.InstanceOf<IListDisplayable>());

    [Test]
    public void TryCreateMatcher_LessOrEqual_ComparesAmount()
    {
        var def = new WeightFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.LessOrEqual, ["250"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new WeightFieldValue { FieldDefinitionId = def.Id, Amount = 250m }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new WeightFieldValue { FieldDefinitionId = def.Id, Amount = 251m }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new WeightFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Greater));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new WeightFieldValue { Amount = 250m, Unit = "g" }), Is.EqualTo(250m));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Greater, ["250 g"], out var withUnit, out _), Is.True);
        Assert.That(withUnit, Is.Not.Null);
    }
}
