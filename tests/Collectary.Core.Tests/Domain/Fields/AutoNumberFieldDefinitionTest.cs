using Collectary.Core.Domain.Fields;

namespace Collectary.Core.Tests.Domain.Fields;

[TestFixture]
public class AutoNumberFieldDefinitionTest
{
    [Test]
    public void CreateEmptyValue_ReturnsTypedValueWithDefinitionId()
    {
        var def = new AutoNumberFieldDefinition();
        var value = def.CreateEmptyValue();
        Assert.That(value, Is.TypeOf<AutoNumberFieldValue>());
        Assert.That(value.FieldDefinitionId, Is.EqualTo(def.Id));
    }

    [Test]
    public void NextNumber_EmptySet_IsOne([Values(AutoNumberStrategy.HighestPlusOne, AutoNumberStrategy.FillGaps)] AutoNumberStrategy strategy)
    {
        var def = new AutoNumberFieldDefinition { Strategy = strategy };
        Assert.That(def.NextNumber(System.Array.Empty<int>()), Is.EqualTo(1));
    }

    [Test]
    public void NextNumber_HighestPlusOne_IgnoresGapsAndTakesMaxPlusOne()
    {
        var def = new AutoNumberFieldDefinition { Strategy = AutoNumberStrategy.HighestPlusOne };
        Assert.That(def.NextNumber(new[] { 1, 2, 5 }), Is.EqualTo(6));
    }

    [Test]
    public void NextNumber_FillGaps_TakesLowestFreeNumber()
    {
        var def = new AutoNumberFieldDefinition { Strategy = AutoNumberStrategy.FillGaps };
        Assert.Multiple(() =>
        {
            Assert.That(def.NextNumber(new[] { 1, 2, 4 }), Is.EqualTo(3));
            Assert.That(def.NextNumber(new[] { 1, 2, 3 }), Is.EqualTo(4));
            Assert.That(def.NextNumber(new[] { 2, 3 }), Is.EqualTo(1));
        });
    }

    [Test]
    public void ApplyTypeSpecificProperties_CopiesConfig()
    {
        var target = new AutoNumberFieldDefinition();
        target.ApplyTypeSpecificProperties(new AutoNumberFieldDefinition
        {
            Editable = true,
            Strategy = AutoNumberStrategy.FillGaps,
            OnDuplicate = DuplicateHandling.Warn,
        });
        Assert.Multiple(() =>
        {
            Assert.That(target.Editable, Is.True);
            Assert.That(target.Strategy, Is.EqualTo(AutoNumberStrategy.FillGaps));
            Assert.That(target.OnDuplicate, Is.EqualTo(DuplicateHandling.Warn));
        });
    }

    [Test]
    public void ApplyTypeSpecificProperties_IgnoresForeignType()
    {
        var target = new AutoNumberFieldDefinition { Editable = true, Strategy = AutoNumberStrategy.FillGaps };
        target.ApplyTypeSpecificProperties(new TextFieldDefinition());
        Assert.Multiple(() =>
        {
            Assert.That(target.Editable, Is.True);
            Assert.That(target.Strategy, Is.EqualTo(AutoNumberStrategy.FillGaps));
        });
    }

    [Test]
    public void Defaults_AreReadOnly_HighestPlusOne_ErrorOnDuplicate()
    {
        var def = new AutoNumberFieldDefinition();
        Assert.Multiple(() =>
        {
            Assert.That(def.Editable, Is.False);
            Assert.That(def.Strategy, Is.EqualTo(AutoNumberStrategy.HighestPlusOne));
            Assert.That(def.OnDuplicate, Is.EqualTo(DuplicateHandling.Error));
            Assert.That(def.ShowInList, Is.True);
        });
    }
}
