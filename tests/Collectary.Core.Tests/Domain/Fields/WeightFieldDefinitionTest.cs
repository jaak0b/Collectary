using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class WeightFieldDefinitionTest
{
    [Test]
    public void TryImportFromText_ParsesAmountAndUnit()
    {
        var ok = ((ITextImportable)new WeightFieldDefinition()).TryImportFromText("250 g", CultureInfo.InvariantCulture, out var v);
        Assert.That(ok, Is.True);
        var w = (WeightFieldValue)v;
        Assert.That(w.Amount, Is.EqualTo(250m));
        Assert.That(w.Unit, Is.EqualTo("g"));
    }

    [Test]
    public void TryImportFromText_RejectsGibberish()
    {
        var ok = ((ITextImportable)new WeightFieldDefinition()).TryImportFromText("heavy", CultureInfo.InvariantCulture, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new WeightFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<WeightFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void IsListDisplayable() =>
        Assert.That(new WeightFieldDefinition(), Is.InstanceOf<IListDisplayable>());
}
