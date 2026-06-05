using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class BarcodeFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new BarcodeFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<BarcodeFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new BarcodeFieldDefinition(), Is.InstanceOf<IListDisplayable>());
}
