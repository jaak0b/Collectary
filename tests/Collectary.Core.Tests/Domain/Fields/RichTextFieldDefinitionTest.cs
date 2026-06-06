using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class RichTextFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_StoresText()
    {
        var ok = ((ITextImportable)new RichTextFieldDefinition()).TryImportFromText("<b>hi</b>", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((RichTextFieldValue)v).Value, Is.EqualTo("<b>hi</b>"));
    }

    [Test]
    public void TryImportFromText_RejectsWhitespace()
    {
        var ok = ((ITextImportable)new RichTextFieldDefinition()).TryImportFromText("  ", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void ImportInferenceOrder_IsLast() =>
        Assert.That(((ITextImportable)new RichTextFieldDefinition()).ImportInferenceOrder, Is.EqualTo(int.MaxValue));

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new RichTextFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<RichTextFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }
}
