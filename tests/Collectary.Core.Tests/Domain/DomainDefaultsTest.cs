using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain;

[TestFixture]
public class DomainDefaultsTest
{
    [Test]
    public void Item_DisplayName_DefaultsToEmptyString() =>
        Assert.That(new Item().DisplayName, Is.EqualTo(string.Empty));

    [Test]
    public void Preset_Name_DefaultsToEmptyString() =>
        Assert.That(new Preset().Name, Is.EqualTo(string.Empty));

    [Test]
    public void SharedField_Name_DefaultsToEmptyString() =>
        Assert.That(new SharedField { Definition = new TextFieldDefinition() }.Name, Is.EqualTo(string.Empty));

    [Test]
    public void FieldGroup_Name_DefaultsToEmptyString() =>
        Assert.That(new FieldGroup().Name, Is.EqualTo(string.Empty));

    [Test]
    public void FieldGroup_ShowInList_DefaultsToTrue() =>
        Assert.That(new FieldGroup().ShowInList, Is.True);

    [Test]
    public void ChoiceOption_Value_DefaultsToEmptyString() =>
        Assert.That(new ChoiceOption().Value, Is.EqualTo(string.Empty));

    [Test]
    public void FieldDefinition_Label_DefaultsToEmptyString() =>
        Assert.That(new TextFieldDefinition().Label, Is.EqualTo(string.Empty));
}

[TestFixture]
public class FieldDefinitionGetOrCreateTest
{
    [Test]
    public void GetOrCreateEmptyValue_WithNull_ReturnsNewValueWithDefinitionId()
    {
        var def = new TextFieldDefinition();

        var result = def.GetOrCreateEmptyValue(null);

        Assert.That(result.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void GetOrCreateEmptyValue_WithMatchingType_ReturnsExisting()
    {
        var def = new TextFieldDefinition();
        var existing = new TextFieldValue { FieldDefinitionId = def.Id };

        var result = def.GetOrCreateEmptyValue(existing);

        Assert.That(result, Is.SameAs(existing));
    }

    [Test]
    public void GetOrCreateEmptyValue_WithWrongType_ThrowsWithTypeName()
    {
        var def = new TextFieldDefinition();
        var wrongValue = new BoolFieldValue();

        var ex = Assert.Throws<InvalidOperationException>(() => def.GetOrCreateEmptyValue(wrongValue));
        Assert.That(ex!.Message, Does.Contain("TextFieldValue"));
        Assert.That(ex.Message, Does.Contain("BoolFieldValue"));
    }

    [Test]
    public void BaseGetOrCreateEmptyValue_WithNull_ReturnsNewValue()
    {
        FieldDefinition def = new TextFieldDefinition();

        var result = def.GetOrCreateEmptyValue(null);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void BaseGetOrCreateEmptyValue_WithWrongType_ThrowsWithTypeName()
    {
        FieldDefinition def = new TextFieldDefinition();
        var wrongValue = new BoolFieldValue();

        var ex = Assert.Throws<InvalidOperationException>(() => def.GetOrCreateEmptyValue(wrongValue));
        Assert.That(ex!.Message, Does.Contain("TextFieldValue"));
        Assert.That(ex.Message, Does.Contain("BoolFieldValue"));
    }
}
