using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class DisplayNameFieldDefinitionTest
{
    [Test]
    public void ValueType_IsTextFieldValue() =>
        Assert.That(new DisplayNameFieldDefinition().ValueType, Is.EqualTo(typeof(TextFieldValue)));

    [Test]
    public void ShowInList_DefaultsToTrue() =>
        Assert.That(new DisplayNameFieldDefinition().ShowInList, Is.True);

    [Test]
    public void CreateEmptyValue_Throws() =>
        Assert.Throws<NotSupportedException>(() => new DisplayNameFieldDefinition().CreateEmptyValue());

    [Test]
    public void IsTitleField_IsTrue() =>
        Assert.That(new DisplayNameFieldDefinition().IsTitleField, Is.True);

    [Test]
    public void IsTitleField_IsFalse_ForNonTitleType() =>
        Assert.That(new TextFieldDefinition().IsTitleField, Is.False);
}
