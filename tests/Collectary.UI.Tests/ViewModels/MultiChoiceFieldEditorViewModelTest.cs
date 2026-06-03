using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class MultiChoiceFieldEditorViewModelTest
{
    [Test]
    public void PreSelectsAndPersistsCheckedItems()
    {
        var def = new MultiChoiceFieldDefinition
        {
            Choices =
            {
                new ChoiceOption { Value = "A", DisplayOrder = 0 },
                new ChoiceOption { Value = "B", DisplayOrder = 1 },
                new ChoiceOption { Value = "C", DisplayOrder = 2 }
            }
        };
        var sut = new MultiChoiceFieldEditorViewModel(def, new MultiChoiceFieldValue { Selected = { "B" } });

        Assert.That(sut.ChoiceItems.Single(c => c.Label == "B").IsSelected, Is.True);
        Assert.That(sut.ChoiceItems.Single(c => c.Label == "A").IsSelected, Is.False);

        sut.ChoiceItems.Single(c => c.Label == "C").IsSelected = true;
        Assert.That(((MultiChoiceFieldValue)sut.GetCurrentValue()).Selected, Is.EqualTo(new[] { "B", "C" }));
    }
}
