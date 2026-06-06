using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class UrlFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_AcceptsAbsoluteUrl()
    {
        var ok = ((ITextImportable)new UrlFieldDefinition()).TryImportFromText("https://example.com", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((UrlFieldValue)v).Url, Is.EqualTo("https://example.com"));
    }

    [Test]
    public void TryImportFromText_RejectsPlainText()
    {
        var ok = ((ITextImportable)new UrlFieldDefinition()).TryImportFromText("not a url", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new UrlFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<UrlFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }
}
