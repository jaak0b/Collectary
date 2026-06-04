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

    [Test]
    public void DefaultColumnSpan_IsTwo() =>
        Assert.That(new ImageFieldDefinition().DefaultColumnSpan, Is.EqualTo(2));

    [Test]
    public void ApplyTypeSpecificProperties_CopiesSizing()
    {
        var target = new ImageFieldDefinition();
        target.ApplyTypeSpecificProperties(new ImageFieldDefinition
        {
            DisplayWidth = 320,
            DisplayHeight = 240,
            SizeMode = ImageSizeMode.Max
        });
        Assert.That(target.DisplayWidth, Is.EqualTo(320));
        Assert.That(target.DisplayHeight, Is.EqualTo(240));
        Assert.That(target.SizeMode, Is.EqualTo(ImageSizeMode.Max));
    }

    [Test]
    public void ApplyTypeSpecificProperties_IgnoresForeignType()
    {
        var target = new ImageFieldDefinition { DisplayWidth = 50 };
        target.ApplyTypeSpecificProperties(new TextFieldDefinition());
        Assert.That(target.DisplayWidth, Is.EqualTo(50));
    }
}
