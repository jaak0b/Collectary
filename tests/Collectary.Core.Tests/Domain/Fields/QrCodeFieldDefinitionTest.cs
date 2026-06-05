using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class QrCodeFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new QrCodeFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<QrCodeFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new QrCodeFieldDefinition(), Is.InstanceOf<IListDisplayable>());
}
