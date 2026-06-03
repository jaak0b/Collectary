using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class ListFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new ListFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<ListFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void DefaultsToCardInlineStyle() =>
        Assert.That(new ListFieldDefinition().InlineStyle, Is.EqualTo(ListInlineStyle.Card));
}
