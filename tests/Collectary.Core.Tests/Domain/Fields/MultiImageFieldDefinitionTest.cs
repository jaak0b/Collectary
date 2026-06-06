using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class MultiImageFieldDefinitionTest
{
    [Test]
    public void IsNotTextImportable() =>
        Assert.That(new MultiImageFieldDefinition() is ITextImportable, Is.False);

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new MultiImageFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<MultiImageFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }
}
