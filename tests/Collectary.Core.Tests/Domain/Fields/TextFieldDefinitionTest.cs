using System.Globalization;
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

    [Test]
    public void ApplyTypeSpecificProperties_CopiesMaxLength()
    {
        var target = new TextFieldDefinition { MaxLength = null };
        target.ApplyTypeSpecificProperties(new TextFieldDefinition { MaxLength = 50 });
        Assert.That(target.MaxLength, Is.EqualTo(50));
    }

    [Test]
    public void ApplyTypeSpecificProperties_IgnoresForeignType()
    {
        var target = new TextFieldDefinition { MaxLength = 12 };
        target.ApplyTypeSpecificProperties(new IntegerFieldDefinition());
        Assert.That(target.MaxLength, Is.EqualTo(12));
    }

    [Test]
    public void TryImportFromText_AcceptsAnyNonEmptyText()
    {
        var def = new TextFieldDefinition();
        var ok = ((ITextImportable)def).TryImportFromText("hello", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((TextFieldValue)v).Value, Is.EqualTo("hello"));
        Assert.That(v.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void TryImportFromText_RejectsWhitespace()
    {
        var ok = ((ITextImportable)new TextFieldDefinition()).TryImportFromText("   ", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void ImportInferenceOrder_IsCatchAllLast() =>
        Assert.That(((ITextImportable)new TextFieldDefinition()).ImportInferenceOrder, Is.EqualTo(int.MaxValue));
}
