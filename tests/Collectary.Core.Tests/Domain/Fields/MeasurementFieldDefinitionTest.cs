using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class MeasurementFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_ParsesAmountAndUnit()
    {
        var ok = ((ITextImportable)new MeasurementFieldDefinition()).TryImportFromText("50 mm", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        var m = (MeasurementFieldValue)v;
        Assert.That(m.Amount, Is.EqualTo(50m));
        Assert.That(m.Unit, Is.EqualTo("mm"));
    }

    [Test]
    public void TryImportFromText_RejectsNumberWithoutUnit()
    {
        var ok = ((ITextImportable)new MeasurementFieldDefinition()).TryImportFromText("50", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new MeasurementFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<MeasurementFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new MeasurementFieldDefinition(), Is.InstanceOf<IListDisplayable>());

    [Test]
    public void TryCreateMatcher_Greater_ComparesAmountFromPlainNumberOrAmountWithUnit()
    {
        var def = new MeasurementFieldDefinition();
        var item = new Item
        {
            Values = [new MeasurementFieldValue { FieldDefinitionId = def.Id, Amount = 12m, Unit = "mm" }],
        };
        ISearchableFieldDefinition search = def;

        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Greater, ["10"], out var plain, out _), Is.True);
        Assert.That(plain!.Matches(item, [def.Id]), Is.True);

        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Greater, ["10 mm"], out var withUnit, out _), Is.True);
        Assert.That(withUnit!.Matches(item, [def.Id]), Is.True);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new MeasurementFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Less));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new MeasurementFieldValue { Amount = 12m }), Is.EqualTo(12m));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }

    [Test]
    public void TryCreateMatcher_OperandWithUnit_MatchesOnlyValuesInThatUnit()
    {
        var def = new MeasurementFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["12 mm"], out var matcher, out _), Is.True);

        var millimeters = new Item { Values = [new MeasurementFieldValue { FieldDefinitionId = def.Id, Amount = 12m, Unit = "mm" }] };
        var centimeters = new Item { Values = [new MeasurementFieldValue { FieldDefinitionId = def.Id, Amount = 12m, Unit = "cm" }] };
        Assert.That(matcher!.Matches(millimeters, [def.Id]), Is.True);
        Assert.That(matcher.Matches(centimeters, [def.Id]), Is.False);
    }

    [Test]
    public void TryCreateMatcher_GreaterWithUnit_RestrictsToTheUnit()
    {
        var def = new MeasurementFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Greater, ["10 mm"], out var matcher, out _), Is.True);

        var centimeters = new Item { Values = [new MeasurementFieldValue { FieldDefinitionId = def.Id, Amount = 12m, Unit = "cm" }] };
        Assert.That(matcher!.Matches(centimeters, [def.Id]), Is.False);
    }
}
