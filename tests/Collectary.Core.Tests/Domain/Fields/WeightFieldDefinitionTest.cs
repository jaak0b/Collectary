using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class WeightFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new WeightFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<WeightFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new WeightFieldDefinition(), Is.InstanceOf<IListDisplayable>());
}
