using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class IntegerFieldEditorViewModelTest
{
    [Test]
    public void LoadsAndPersists()
    {
        var sut = new IntegerFieldEditorViewModel(new IntegerFieldDefinition(), new IntegerFieldValue { Value = 3 });
        Assert.That(sut.Number, Is.EqualTo(3));
        sut.Number = 9;
        Assert.That(((IntegerFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo(9));
    }

    [Test]
    public void MinimumMaximum_ReflectDefinition()
    {
        var sut = new IntegerFieldEditorViewModel(
            new IntegerFieldDefinition { Min = 2, Max = 8 }, new IntegerFieldValue());
        Assert.That(sut.Minimum, Is.EqualTo(2m));
        Assert.That(sut.Maximum, Is.EqualTo(8m));
    }

    [Test]
    public void MinimumMaximum_FallBackToIntBounds_WhenUnset()
    {
        var sut = new IntegerFieldEditorViewModel(new IntegerFieldDefinition(), new IntegerFieldValue());
        Assert.That(sut.Minimum, Is.EqualTo((decimal)int.MinValue));
        Assert.That(sut.Maximum, Is.EqualTo((decimal)int.MaxValue));
    }
}
