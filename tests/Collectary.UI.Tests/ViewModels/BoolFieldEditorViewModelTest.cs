using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class BoolFieldEditorViewModelTest
{
    [Test]
    public void LoadsFalseFromNull_AndPersists()
    {
        var sut = new BoolFieldEditorViewModel(new BoolFieldDefinition(), new BoolFieldValue { Value = null });
        Assert.That(sut.IsChecked, Is.False);
        sut.IsChecked = true;
        Assert.That(((BoolFieldValue)sut.GetCurrentValue()).Value, Is.True);
    }
}
