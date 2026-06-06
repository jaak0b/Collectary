using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class AudioFieldDefinitionTest
{
    [Test]
    public void IsNotTextImportable() =>
        Assert.That(new AudioFieldDefinition() is ITextImportable, Is.False);

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new AudioFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<AudioFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }
}
