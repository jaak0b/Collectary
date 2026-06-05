using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class ProgressFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new ProgressFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<ProgressFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new ProgressFieldDefinition(), Is.InstanceOf<IListDisplayable>());
}
