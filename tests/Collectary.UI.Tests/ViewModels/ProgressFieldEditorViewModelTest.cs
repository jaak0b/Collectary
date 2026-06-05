using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ProgressFieldEditorViewModelTest
{
    [Test]
    public void LoadsHaveAndTotal()
    {
        var sut = new ProgressFieldEditorViewModel(new ProgressFieldDefinition(),
            new ProgressFieldValue { Have = 3, Total = 12 });
        Assert.That(sut.Have, Is.EqualTo(3));
        Assert.That(sut.Total, Is.EqualTo(12));
    }

    [Test]
    public void Fraction_AndPercent_Computed()
    {
        var sut = new ProgressFieldEditorViewModel(new ProgressFieldDefinition(),
            new ProgressFieldValue { Have = 1, Total = 4 });
        Assert.That(sut.Fraction, Is.EqualTo(0.25).Within(1e-9));
        Assert.That(sut.Percent, Is.EqualTo(25));
    }

    [Test]
    public void Fraction_ZeroWhenNoTotal()
    {
        var sut = new ProgressFieldEditorViewModel(new ProgressFieldDefinition(),
            new ProgressFieldValue { Have = 5, Total = 0 });
        Assert.That(sut.Fraction, Is.EqualTo(0));
        Assert.That(sut.Percent, Is.EqualTo(0));
    }

    [Test]
    public void Fraction_ClampedWhenHaveExceedsTotal()
    {
        var sut = new ProgressFieldEditorViewModel(new ProgressFieldDefinition(),
            new ProgressFieldValue { Have = 20, Total = 10 });
        Assert.That(sut.Fraction, Is.EqualTo(1));
    }

    [Test]
    public void GetCurrentValue_PersistsHaveAndTotal()
    {
        var sut = new ProgressFieldEditorViewModel(new ProgressFieldDefinition(), new ProgressFieldValue());
        sut.Have = 7;
        sut.Total = 50;
        var v = (ProgressFieldValue)sut.GetCurrentValue();
        Assert.That(v.Have, Is.EqualTo(7));
        Assert.That(v.Total, Is.EqualTo(50));
    }
}
