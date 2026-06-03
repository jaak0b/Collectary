using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class DecimalFieldEditorViewModelTest
{
    [Test]
    public void LoadsAndPersists()
    {
        var sut = new DecimalFieldEditorViewModel(new DecimalFieldDefinition(), new DecimalFieldValue { Value = 1.5m });
        Assert.That(sut.Number, Is.EqualTo(1.5m));
        sut.Number = 2.25m;
        Assert.That(((DecimalFieldValue)sut.GetCurrentValue()).Value, Is.EqualTo(2.25m));
    }
}
