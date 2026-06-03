using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels;

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
}
