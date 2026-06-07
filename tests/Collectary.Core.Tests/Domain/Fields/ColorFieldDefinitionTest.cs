using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class ColorFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_AcceptsHexColor()
    {
        var ok = ((ITextImportable)new ColorFieldDefinition()).TryImportFromText("#1a2b3c", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((ColorFieldValue)v).Value, Is.EqualTo("#1a2b3c"));
    }

    [Test]
    public void TryImportFromText_RejectsNonColor()
    {
        var ok = ((ITextImportable)new ColorFieldDefinition()).TryImportFromText("reddish", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryImportFromText_RejectsMalformedRgb()
    {
        var ok = ((ITextImportable)new ColorFieldDefinition()).TryImportFromText("rgb(banana)", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryImportFromText_AcceptsValidRgb()
    {
        var ok = ((ITextImportable)new ColorFieldDefinition()).TryImportFromText("rgb(255, 0, 0)", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((ColorFieldValue)v).Value, Is.EqualTo("rgb(255, 0, 0)"));
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new ColorFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<ColorFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void DefaultsToHexFormat() =>
        Assert.That(new ColorFieldDefinition().Format, Is.EqualTo(ColorFormat.Hex));

    [Test]
    public void DefaultColumnSpan_IsTwo() =>
        Assert.That(new ColorFieldDefinition().DefaultColumnSpan, Is.EqualTo(2));

    [Test]
    public void ApplyTypeSpecificProperties_CopiesFormat()
    {
        var target = new ColorFieldDefinition { Format = ColorFormat.Hex };
        target.ApplyTypeSpecificProperties(new ColorFieldDefinition { Format = ColorFormat.Rgb });
        Assert.That(target.Format, Is.EqualTo(ColorFormat.Rgb));
    }

    [Test]
    public void ApplyTypeSpecificProperties_IgnoresForeignType()
    {
        var target = new ColorFieldDefinition { Format = ColorFormat.Rgb };
        target.ApplyTypeSpecificProperties(new TextFieldDefinition());
        Assert.That(target.Format, Is.EqualTo(ColorFormat.Rgb));
    }
}
