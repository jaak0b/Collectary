using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class DateFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new DateFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<DateFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void TryImportFromText_HonoursDateFormatPerCulture()
    {
        var de = ((ITextImportable)new DateFieldDefinition()).TryImportFromText("31.12.2024", new CultureInfo("de-DE"), out var v);
        Assert.That(de, Is.True);
        Assert.That(((DateFieldValue)v).Value, Is.EqualTo(new DateTime(2024, 12, 31)));

        var en = ((ITextImportable)new DateFieldDefinition()).TryImportFromText("12/31/2024", new CultureInfo("en-US"), out var v2);
        Assert.That(en, Is.True);
        Assert.That(((DateFieldValue)v2).Value, Is.EqualTo(new DateTime(2024, 12, 31)));
    }

    [Test]
    public void TryImportFromText_RejectsNonDate()
    {
        var ok = ((ITextImportable)new DateFieldDefinition()).TryImportFromText("not a date", new CultureInfo("en-US"), out _);
        Assert.That(ok, Is.False);
    }
}
