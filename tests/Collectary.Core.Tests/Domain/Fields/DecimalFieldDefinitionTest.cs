using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class DecimalFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new DecimalFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<DecimalFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void DefaultsToTwoDecimalPlaces() =>
        Assert.That(new DecimalFieldDefinition().DecimalPlaces, Is.EqualTo(2));

    [Test]
    public void ApplyTypeSpecificProperties_CopiesDecimalPlaces()
    {
        var target = new DecimalFieldDefinition { DecimalPlaces = 2 };
        target.ApplyTypeSpecificProperties(new DecimalFieldDefinition { DecimalPlaces = 4 });
        Assert.That(target.DecimalPlaces, Is.EqualTo(4));
    }

    [Test]
    public void ApplyTypeSpecificProperties_IgnoresForeignType()
    {
        var target = new DecimalFieldDefinition { DecimalPlaces = 3 };
        target.ApplyTypeSpecificProperties(new TextFieldDefinition());
        Assert.That(target.DecimalPlaces, Is.EqualTo(3));
    }

    [Test]
    public void TryImportFromText_HonoursDecimalSeparatorPerCulture()
    {
        var de = ((ITextImportable)new DecimalFieldDefinition()).TryImportFromText("1.234,56", new CultureInfo("de-DE"), out var v);
        Assert.That(de, Is.True);
        Assert.That(((DecimalFieldValue)v).Value, Is.EqualTo(1234.56m));

        var en = ((ITextImportable)new DecimalFieldDefinition()).TryImportFromText("1,234.56", new CultureInfo("en-US"), out var v2);
        Assert.That(en, Is.True);
        Assert.That(((DecimalFieldValue)v2).Value, Is.EqualTo(1234.56m));
    }

    [Test]
    public void TryImportFromText_RejectsNonNumber()
    {
        var ok = ((ITextImportable)new DecimalFieldDefinition()).TryImportFromText("abc", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }
}
