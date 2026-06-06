using System.Globalization;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class PercentageFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new PercentageFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<PercentageFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void TryImportFromText_StripsPercentSign()
    {
        var ok = ((ITextImportable)new PercentageFieldDefinition()).TryImportFromText("50%", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        Assert.That(((PercentageFieldValue)v).Value, Is.EqualTo(50m));
    }

    [Test]
    public void TryImportFromText_HonoursCulture()
    {
        var ok = ((ITextImportable)new PercentageFieldDefinition()).TryImportFromText("12,5 %", new CultureInfo("de-DE"), out var v);
        Assert.That(ok, Is.True);
        Assert.That(((PercentageFieldValue)v).Value, Is.EqualTo(12.5m));
    }

    [Test]
    public void TryImportFromText_AcceptsPlainNumberButInfersLast()
    {
        var importable = (ITextImportable)new PercentageFieldDefinition();
        Assert.That(importable.TryImportFromText("75", CultureInfo.InvariantCulture, out _), Is.True);
        Assert.That(importable.ImportInferenceOrder, Is.GreaterThan(((ITextImportable)new DecimalFieldDefinition()).ImportInferenceOrder));
    }
}
