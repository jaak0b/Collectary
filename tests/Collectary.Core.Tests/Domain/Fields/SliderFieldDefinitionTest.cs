using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class SliderFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new SliderFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<SliderFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new SliderFieldDefinition(), Is.InstanceOf<IListDisplayable>());
}
