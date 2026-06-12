using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Search;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class IntegerFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new IntegerFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<IntegerFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void ApplyTypeSpecificProperties_CopiesMinAndMax()
    {
        var target = new IntegerFieldDefinition();
        target.ApplyTypeSpecificProperties(new IntegerFieldDefinition { Min = -3, Max = 12 });
        Assert.That(target.Min, Is.EqualTo(-3));
        Assert.That(target.Max, Is.EqualTo(12));
    }

    [Test]
    public void ApplyTypeSpecificProperties_IgnoresForeignType()
    {
        var target = new IntegerFieldDefinition { Min = 1, Max = 9 };
        target.ApplyTypeSpecificProperties(new TextFieldDefinition());
        Assert.That(target.Min, Is.EqualTo(1));
        Assert.That(target.Max, Is.EqualTo(9));
    }

    [Test]
    public void TryImportFromText_ParsesPlainInteger()
    {
        var ok = ((ITextImportable)new IntegerFieldDefinition()).TryImportFromText("42", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((IntegerFieldValue)v).Value, Is.EqualTo(42));
    }

    [Test]
    public void TryImportFromText_HonoursThousandsSeparatorPerCulture()
    {
        var de = ((ITextImportable)new IntegerFieldDefinition()).TryImportFromText("1.234", new CultureInfo("de-DE"), out var v);
        Assert.That(de, Is.True);
        Assert.That(((IntegerFieldValue)v).Value, Is.EqualTo(1234));

        var en = ((ITextImportable)new IntegerFieldDefinition()).TryImportFromText("1,234", new CultureInfo("en-US"), out var v2);
        Assert.That(en, Is.True);
        Assert.That(((IntegerFieldValue)v2).Value, Is.EqualTo(1234));
    }

    [Test]
    public void TryImportFromText_RejectsNonInteger()
    {
        var ok = ((ITextImportable)new IntegerFieldDefinition()).TryImportFromText("4.5", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryCreateMatcher_Greater_MatchesLargerStoredNumber()
    {
        var def = new IntegerFieldDefinition();
        ISearchableFieldDefinition search = def;
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Greater, ["5"], out var matcher, out _), Is.True);

        var item = new Item { Values = [new IntegerFieldValue { FieldDefinitionId = def.Id, Value = 6 }] };
        Assert.That(matcher!.Matches(item, [def.Id]), Is.True);
        item.Values = [new IntegerFieldValue { FieldDefinitionId = def.Id, Value = 5 }];
        Assert.That(matcher.Matches(item, [def.Id]), Is.False);
    }

    [Test]
    public void TryCreateMatcher_NonNumericOperand_ReportsInvalidValue()
    {
        ISearchableFieldDefinition search = new IntegerFieldDefinition();
        Assert.That(search.TryCreateMatcher(QueryOperatorKind.Equals, ["abc"], out _, out var error), Is.False);
        Assert.That(error, Is.EqualTo(QueryErrorCode.InvalidValue));
    }

    [Test]
    public void SortKey_ReturnsStoredNumber()
    {
        ISearchableFieldDefinition search = new IntegerFieldDefinition();
        Assert.That(search.SortKey(new Item(), new IntegerFieldValue { Value = 7 }), Is.EqualTo(7));
    }

    [Test]
    public void SearchSurface_ExposesOperatorsAndNoSuggestions()
    {
        ISearchableFieldDefinition search = new IntegerFieldDefinition();
        Assert.That(search.SupportedOperators, Does.Contain(QueryOperatorKind.Less));
        Assert.That(search.ValueSuggestions(), Is.Empty);
    }
}
