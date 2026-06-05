using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class MeasurementFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new MeasurementFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<MeasurementFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new MeasurementFieldDefinition(), Is.InstanceOf<IListDisplayable>());
}
