using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class TextFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new TextFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<TextFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void ValueType_IsTextFieldValue() =>
        Assert.That(new TextFieldDefinition().ValueType, Is.EqualTo(typeof(TextFieldValue)));

    [Test]
    public void GetOrCreateEmptyValue_CreatesNew_WhenExistingNull()
    {
        var def = new TextFieldDefinition();
        var value = ((FieldDefinition)def).GetOrCreateEmptyValue(null);
        Assert.That(value, Is.TypeOf<TextFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void GetOrCreateEmptyValue_ReturnsExisting_WhenTypeMatches()
    {
        var def = new TextFieldDefinition();
        var existing = new TextFieldValue { Value = "x" };
        Assert.That(((FieldDefinition)def).GetOrCreateEmptyValue(existing), Is.SameAs(existing));
    }

    [Test]
    public void GetOrCreateEmptyValue_Throws_WhenTypeMismatch()
    {
        var def = new TextFieldDefinition();
        Assert.Throws<InvalidOperationException>(
            () => ((FieldDefinition)def).GetOrCreateEmptyValue(new IntegerFieldValue()));
    }

    [Test]
    public void GenericGetOrCreateEmptyValue_NewExistingThrow()
    {
        var def = new TextFieldDefinition();
        Assert.That(def.GetOrCreateEmptyValue(null), Is.TypeOf<TextFieldValue>());
        var existing = new TextFieldValue();
        Assert.That(def.GetOrCreateEmptyValue(existing), Is.SameAs(existing));
        Assert.Throws<InvalidOperationException>(() => def.GetOrCreateEmptyValue(new BoolFieldValue()));
    }
}
