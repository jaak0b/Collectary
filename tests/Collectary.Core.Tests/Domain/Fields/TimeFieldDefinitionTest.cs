using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class TimeFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_AcceptsTimeOfDay()
    {
        var ok = ((ITextImportable)new TimeFieldDefinition()).TryImportFromText("14:30", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((TimeFieldValue)v).Value, Is.EqualTo("14:30"));
    }

    [Test]
    public void TryImportFromText_RejectsNonTime()
    {
        var ok = ((ITextImportable)new TimeFieldDefinition()).TryImportFromText("hello", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new TimeFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<TimeFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }
}
