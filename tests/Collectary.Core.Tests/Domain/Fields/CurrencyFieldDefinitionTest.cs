using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class CurrencyFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new CurrencyFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<CurrencyFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void DefaultsToEuroSymbol() =>
        Assert.That(new CurrencyFieldDefinition().CurrencySymbol, Is.EqualTo("€"));

    [Test]
    public void TryImportFromText_ParsesPlainNumber()
    {
        var ok = ((ITextImportable)new CurrencyFieldDefinition()).TryImportFromText("5.50", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((CurrencyFieldValue)v).Value, Is.EqualTo(5.50m));
    }

    [Test]
    public void TryImportFromText_StripsCurrencySymbolPerCulture()
    {
        var de = ((ITextImportable)new CurrencyFieldDefinition()).TryImportFromText("5,50 €", new CultureInfo("de-DE"), out var v);
        Assert.That(de, Is.True);
        Assert.That(((CurrencyFieldValue)v).Value, Is.EqualTo(5.50m));

        var en = ((ITextImportable)new CurrencyFieldDefinition()).TryImportFromText("$5.50", new CultureInfo("en-US"), out var v2);
        Assert.That(en, Is.True);
        Assert.That(((CurrencyFieldValue)v2).Value, Is.EqualTo(5.50m));
    }

    [Test]
    public void TryImportFromText_RejectsParenthesizedAccountingNegative()
    {
        var ok = ((ITextImportable)new CurrencyFieldDefinition()).TryImportFromText("(123)", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryImportFromText_RejectsNonNumber()
    {
        var ok = ((ITextImportable)new CurrencyFieldDefinition()).TryImportFromText("free", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryCreateMatcher_Equals_AcceptsPlainAndSymbolPrefixedAmounts()
    {
        var def = new CurrencyFieldDefinition();
        var item = new Item { Values = [new CurrencyFieldValue { FieldDefinitionId = def.Id, Value = 5.50m }] };
        ISearchableFieldDefinition search = def;

        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["5.50"], out var plain, out _), Is.True);
        Assert.That(plain!.Matches(item, [def.Id]), Is.True);

        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["€5.50"], out var symbol, out _), Is.True);
        Assert.That(symbol!.Matches(item, [def.Id]), Is.True);
    }

    [Test]
    public void SearchSurface_ExposesOperatorsSuggestionsAndSortKey()
    {
        ISearchableFieldDefinition search = new CurrencyFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Greater));
        Assert.That(search.ValueSuggestions(), Is.Empty);
        Assert.That(search.SortKey(new Item(), new CurrencyFieldValue { Value = 5.5m }), Is.EqualTo(5.5m));
        Assert.That(search.SortKey(new Item(), null), Is.Null);
    }
}
