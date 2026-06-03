using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class RatingFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new RatingFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<RatingFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void DefaultsToFiveStars() =>
        Assert.That(new RatingFieldDefinition().MaxStars, Is.EqualTo(5));
}
