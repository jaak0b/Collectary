using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SingleChoiceFieldEditorViewModelTest
{
    [Test]
    public void ExposesOrderedChoices_LoadsAndPersists()
    {
        var def = new SingleChoiceFieldDefinition
        {
            Choices =
            {
                new ChoiceOption { Value = "B", DisplayOrder = 1 },
                new ChoiceOption { Value = "A", DisplayOrder = 0 }
            }
        };
        var sut = new SingleChoiceFieldEditorViewModel(def, new SingleChoiceFieldValue { Selected = "A" });
        Assert.That(sut.Choices, Is.EqualTo(new[] { "A", "B" }));
        Assert.That(sut.Selected, Is.EqualTo("A"));

        sut.Selected = "B";
        Assert.That(((SingleChoiceFieldValue)sut.GetCurrentValue()).Selected, Is.EqualTo("B"));
    }
}
