using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class CurrencyFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new CurrencyFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<CurrencyFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void DefaultsToEuroSymbol() =>
        Assert.That(new CurrencyFieldDefinition().CurrencySymbol, Is.EqualTo("€"));
}
