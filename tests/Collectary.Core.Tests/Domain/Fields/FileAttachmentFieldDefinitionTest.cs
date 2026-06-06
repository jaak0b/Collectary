using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class FileAttachmentFieldDefinitionTest
{
    [Test]
    public void IsNotTextImportable() =>
        Assert.That(new FileAttachmentFieldDefinition() is ITextImportable, Is.False);

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new FileAttachmentFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<FileAttachmentFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }
}
