using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class DateRangeFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new DateRangeFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<DateRangeFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new DateRangeFieldDefinition(), Is.InstanceOf<IListDisplayable>());
}
