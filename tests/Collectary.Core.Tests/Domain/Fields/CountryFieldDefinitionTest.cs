using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class CountryFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new CountryFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<CountryFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new CountryFieldDefinition(), Is.InstanceOf<IListDisplayable>());
}
