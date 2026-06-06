using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class PhoneFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_AcceptsPhoneNumber()
    {
        var ok = ((ITextImportable)new PhoneFieldDefinition()).TryImportFromText("+49 170 1234567", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((PhoneFieldValue)v).Value, Is.EqualTo("+49 170 1234567"));
    }

    [Test]
    public void TryImportFromText_RejectsLetters()
    {
        var ok = ((ITextImportable)new PhoneFieldDefinition()).TryImportFromText("call me", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new PhoneFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<PhoneFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }
}
