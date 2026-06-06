using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class BoolFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new BoolFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<BoolFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void ThreeState_DefaultsToFalse() =>
        Assert.That(new BoolFieldDefinition().ThreeState, Is.False);

    [Test]
    public void ApplyTypeSpecificProperties_CopiesThreeState()
    {
        var target = new BoolFieldDefinition { ThreeState = false };
        target.ApplyTypeSpecificProperties(new BoolFieldDefinition { ThreeState = true });
        Assert.That(target.ThreeState, Is.True);
    }

    [Test]
    public void ApplyTypeSpecificProperties_IgnoresForeignType()
    {
        var target = new BoolFieldDefinition { ThreeState = true };
        target.ApplyTypeSpecificProperties(new TextFieldDefinition());
        Assert.That(target.ThreeState, Is.True);
    }
}
