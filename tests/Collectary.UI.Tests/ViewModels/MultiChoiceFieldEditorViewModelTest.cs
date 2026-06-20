using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;

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

    [Test]
    public void IsCollapsed_ReflectsDefinitionDisplayMode()
    {
        var expanded = new MultiChoiceFieldEditorViewModel(
            new MultiChoiceFieldDefinition { DisplayMode = MultiChoiceDisplayMode.Expanded }, new MultiChoiceFieldValue());
        var collapsed = new MultiChoiceFieldEditorViewModel(
            new MultiChoiceFieldDefinition { DisplayMode = MultiChoiceDisplayMode.Collapsed }, new MultiChoiceFieldValue());

        Assert.That(expanded.IsCollapsed, Is.False);
        Assert.That(collapsed.IsCollapsed, Is.True);
    }

    [Test]
    public void SelectionDisplay_PlaceholderWhenEmpty_ThenJoinsSelectedLabels()
    {
        var def = new MultiChoiceFieldDefinition
        {
            Choices =
            {
                new ChoiceOption { Value = "A", DisplayOrder = 0 },
                new ChoiceOption { Value = "B", DisplayOrder = 1 }
            }
        };
        var sut = new MultiChoiceFieldEditorViewModel(def, new MultiChoiceFieldValue());
        var changes = 0;
        sut.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(sut.SelectionDisplay)) changes++; };

        Assert.That(sut.SelectionDisplay, Is.EqualTo(LocalizationService.Instance["MultiChoice_SelectPlaceholder"]));

        sut.ChoiceItems.Single(c => c.Label == "A").IsSelected = true;
        Assert.That(sut.SelectionDisplay, Is.EqualTo("A"));

        sut.ChoiceItems.Single(c => c.Label == "B").IsSelected = true;
        Assert.That(sut.SelectionDisplay, Is.EqualTo("A, B"));

        Assert.That(changes, Is.GreaterThanOrEqualTo(2));
    }
}
