using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class IntegerFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new IntegerFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<IntegerFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void ApplyTypeSpecificProperties_CopiesMinAndMax()
    {
        var target = new IntegerFieldDefinition();
        target.ApplyTypeSpecificProperties(new IntegerFieldDefinition { Min = -3, Max = 12 });
        Assert.That(target.Min, Is.EqualTo(-3));
        Assert.That(target.Max, Is.EqualTo(12));
    }

    [Test]
    public void ApplyTypeSpecificProperties_IgnoresForeignType()
    {
        var target = new IntegerFieldDefinition { Min = 1, Max = 9 };
        target.ApplyTypeSpecificProperties(new TextFieldDefinition());
        Assert.That(target.Min, Is.EqualTo(1));
        Assert.That(target.Max, Is.EqualTo(9));
    }
}
