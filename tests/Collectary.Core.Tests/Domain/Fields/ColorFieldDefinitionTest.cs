using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class ColorFieldDefinitionTest
{
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
