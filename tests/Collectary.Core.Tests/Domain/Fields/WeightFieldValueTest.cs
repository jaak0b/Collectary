using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class WeightFieldValueTest
{
    [Test]
    public void IsEmpty_WhenAmountNull()
    {
        Assert.That(new WeightFieldValue().IsEmpty, Is.True);
        Assert.That(new WeightFieldValue { Amount = 31.1m }.IsEmpty, Is.False);
    }

    [Test]
    public void ToString_CombinesAmountAndUnit()
    {
        Assert.That(new WeightFieldValue { Amount = 31.1m, Unit = "g" }.ToString(), Is.EqualTo("31.1 g"));
        Assert.That(new WeightFieldValue { Unit = "kg" }.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void CopyFrom_CopiesAmountAndUnit()
    {
        var target = new WeightFieldValue();
        target.CopyFrom(new WeightFieldValue { Amount = 2m, Unit = "lb" });
        Assert.That(target.Amount, Is.EqualTo(2m));
        Assert.That(target.Unit, Is.EqualTo("lb"));
    }

    [Test]
    public void Unit_DefaultsToGrams() =>
        Assert.That(new WeightFieldValue().Unit, Is.EqualTo("g"));
}
