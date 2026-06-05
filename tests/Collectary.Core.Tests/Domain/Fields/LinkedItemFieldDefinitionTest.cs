using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class LinkedItemFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new LinkedItemFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<LinkedItemFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new LinkedItemFieldDefinition(), Is.InstanceOf<IListDisplayable>());
}
