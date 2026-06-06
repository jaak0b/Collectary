using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class DurationFieldDefinitionTest
{
    [TestCase("90", 90)]
    [TestCase("1:30", 90)]
    [TestCase("2 h 15 min", 135)]
    [TestCase("1h30m", 90)]
    public void TryImportFromText_ParsesDurations(string raw, int expectedMinutes)
    {
        var ok = ((ITextImportable)new DurationFieldDefinition()).TryImportFromText(raw, CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((DurationFieldValue)v).TotalMinutes, Is.EqualTo(expectedMinutes));
    }

    [Test]
    public void TryImportFromText_RejectsGibberish()
    {
        var ok = ((ITextImportable)new DurationFieldDefinition()).TryImportFromText("soon", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new DurationFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<DurationFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }
}
