using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class MeasurementFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_ParsesAmountAndUnit()
    {
        var ok = ((ITextImportable)new MeasurementFieldDefinition()).TryImportFromText("50 mm", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        var m = (MeasurementFieldValue)v;
        Assert.That(m.Amount, Is.EqualTo(50m));
        Assert.That(m.Unit, Is.EqualTo("mm"));
    }

    [Test]
    public void TryImportFromText_RejectsNumberWithoutUnit()
    {
        var ok = ((ITextImportable)new MeasurementFieldDefinition()).TryImportFromText("50", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new MeasurementFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<MeasurementFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new MeasurementFieldDefinition(), Is.InstanceOf<IListDisplayable>());
}
