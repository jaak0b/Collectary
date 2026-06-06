using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class EmailFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_AcceptsAddress()
    {
        var ok = ((ITextImportable)new EmailFieldDefinition()).TryImportFromText("a@b.com", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((EmailFieldValue)v).Value, Is.EqualTo("a@b.com"));
    }

    [Test]
    public void TryImportFromText_RejectsNonAddress()
    {
        var ok = ((ITextImportable)new EmailFieldDefinition()).TryImportFromText("nope", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new EmailFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<EmailFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }
}
