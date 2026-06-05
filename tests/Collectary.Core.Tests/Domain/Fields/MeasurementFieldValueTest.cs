using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class MeasurementFieldValueTest
{
    [Test]
    public void IsEmpty_WhenAmountNull()
    {
        Assert.That(new MeasurementFieldValue().IsEmpty, Is.True);
        Assert.That(new MeasurementFieldValue { Amount = 38m }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_CombinesAmountAndUnit()
    {
        Assert.That(new MeasurementFieldValue { Amount = 38m, Unit = "mm" }.ToString(), Is.EqualTo("38 mm"));
        Assert.That(new MeasurementFieldValue { Unit = "cm" }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesAmountAndUnit()
    {
        var target = new MeasurementFieldValue();
        target.CopyFrom(new MeasurementFieldValue { Amount = 4.5m, Unit = "in" });
        Assert.That(target.Amount, Is.EqualTo(4.5m));
        Assert.That(target.Unit, Is.EqualTo("in"));
    }

    [Test]
    public void Unit_DefaultsToMillimetres() =>
        Assert.That(new MeasurementFieldValue().Unit, Is.EqualTo("mm"));
}
