using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class SingleChoiceFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_StoresSelectedValue()
    {
        var ok = ((ITextImportable)new SingleChoiceFieldDefinition()).TryImportFromText("Option A", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((SingleChoiceFieldValue)v).Selected, Is.EqualTo("Option A"));
    }

    [Test]
    public void TryImportFromText_RejectsWhitespace()
    {
        var ok = ((ITextImportable)new SingleChoiceFieldDefinition()).TryImportFromText("  ", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new SingleChoiceFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<SingleChoiceFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }
}
