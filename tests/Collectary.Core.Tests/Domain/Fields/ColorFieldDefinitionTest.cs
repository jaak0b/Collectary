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
}
