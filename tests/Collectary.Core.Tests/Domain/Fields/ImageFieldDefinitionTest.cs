using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class ImageFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new ImageFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<ImageFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void HasSensibleSizingDefaults()
    {
        var def = new ImageFieldDefinition();
        Assert.That(def.DisplayWidth, Is.EqualTo(200));
        Assert.That(def.DisplayHeight, Is.EqualTo(200));
        Assert.That(def.SizeMode, Is.EqualTo(ImageSizeMode.Fixed));
    }
}
