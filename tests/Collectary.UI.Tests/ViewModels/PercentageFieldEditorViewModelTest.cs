using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class PercentageFieldEditorViewModelTest
{
    [Test]
    public void LoadsAndPersists()
    {
        var sut = new PercentageFieldEditorViewModel(new PercentageFieldDefinition(), new PercentageFieldValue { Value = 10m });
        Assert.That(sut.Number, Is.EqualTo(10m));
        sut.Number = 55m;
        Assert.That(((PercentageFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo(55m));
    }
}
