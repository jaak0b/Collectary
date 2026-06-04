using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.ItemEditor;

[TestFixture]
public class ChoiceEditorTest : FlowTestBase
{
    private EffectiveFields MakeEffectiveFields(FieldDefinition field) => new()
    {
        Fields = [field],
        Groups = [],
        GroupByFieldId = new Dictionary<Guid, Guid?>()
    };

    private static SingleChoiceFieldDefinition MakeSingleDef(params string[] choices) =>
        new()
        {
            Label = "Genre",
            Choices = choices.Select((v, i) => new ChoiceOption { Value = v, DisplayOrder = i }).ToList()
        };

    private static MultiChoiceFieldDefinition MakeMultiDef(params string[] choices) =>
        new()
        {
            Label = "Tags",
            Choices = choices.Select((v, i) => new ChoiceOption { Value = v, DisplayOrder = i }).ToList()
        };

    private static Preset MinimalPreset() => new() { Name = "P", ColumnCount = 1 };

    [Test]
    public void SingleChoice_Choices_LoadedInDefinitionOrder()
    {
        var def = MakeSingleDef("C", "A", "B");
        def.Choices[0].DisplayOrder = 2;
        def.Choices[1].DisplayOrder = 0;
        def.Choices[2].DisplayOrder = 1;

        var editor = new SingleChoiceFieldEditorViewModel(def, def.GetOrCreateEmptyValue(null));

        Assert.That(editor.Choices, Is.EqualTo(new[] { "A", "B", "C" }));
    }

    [Test]
    public void SingleChoice_SelectValue_UpdatesSelected()
    {
        var def = MakeSingleDef("A", "B", "C");
        var editor = new SingleChoiceFieldEditorViewModel(def, def.GetOrCreateEmptyValue(null));

        editor.Selected = "B";

        Assert.That(editor.Selected, Is.EqualTo("B"));
    }

    [Test]
    public void SingleChoice_ClearSelection_SetsNull()
    {
        var def = MakeSingleDef("A", "B");
        var value = def.GetOrCreateEmptyValue(null);
        value.Selected = "A";
        var editor = new SingleChoiceFieldEditorViewModel(def, value);

        editor.Selected = null;

        Assert.That(editor.Selected, Is.Null);
    }

    [Test]
    public void SingleChoice_GetCurrentValue_ReflectsSelected()
    {
        var def = MakeSingleDef("A", "B", "C");
        var editor = new SingleChoiceFieldEditorViewModel(def, def.GetOrCreateEmptyValue(null));
        editor.Selected = "C";

        var result = (SingleChoiceFieldValue)editor.GetCurrentValue();

        Assert.That(result.Selected, Is.EqualTo("C"));
    }

    [Test]
    public void MultiChoice_SelectMultiple_AllReflected()
    {
        var def = MakeMultiDef("A", "B", "C");
        var editor = new MultiChoiceFieldEditorViewModel(def, def.GetOrCreateEmptyValue(null));

        editor.ChoiceItems[0].IsSelected = true;
        editor.ChoiceItems[2].IsSelected = true;

        var result = (MultiChoiceFieldValue)editor.GetCurrentValue();
        Assert.That(result.Selected, Does.Contain("A"));
        Assert.That(result.Selected, Does.Contain("C"));
        Assert.That(result.Selected, Does.Not.Contain("B"));
    }

    [Test]
    public void MultiChoice_Deselect_RemovesFromSelected()
    {
        var def = MakeMultiDef("A", "B", "C");
        var value = def.GetOrCreateEmptyValue(null);
        value.Selected = new List<string> { "A", "B", "C" };
        var editor = new MultiChoiceFieldEditorViewModel(def, value);

        editor.ChoiceItems.Single(c => c.Label == "B").IsSelected = false;

        var result = (MultiChoiceFieldValue)editor.GetCurrentValue();
        Assert.That(result.Selected, Does.Contain("A"));
        Assert.That(result.Selected, Does.Contain("C"));
        Assert.That(result.Selected, Does.Not.Contain("B"));
    }

    [Test]
    public void MultiChoice_NoChoices_EditorHasNoItems()
    {
        var def = new MultiChoiceFieldDefinition { Label = "Empty" };
        var editor = new MultiChoiceFieldEditorViewModel(def, def.GetOrCreateEmptyValue(null));

        Assert.That(editor.ChoiceItems, Is.Empty);
    }

    [Test]
    public void SingleChoice_NoChoices_EditorHasEmptyList()
    {
        var def = new SingleChoiceFieldDefinition { Label = "Empty" };
        var editor = new SingleChoiceFieldEditorViewModel(def, def.GetOrCreateEmptyValue(null));

        Assert.That(editor.Choices, Is.Empty);
    }

    [Test]
    public async Task SingleChoice_ChoiceOrder_RoundTrips_ThroughDb()
    {
        var def = new SingleChoiceFieldDefinition
        {
            Label = "Status",
            Choices =
            [
                new ChoiceOption { Value = "Pending", DisplayOrder = 1 },
                new ChoiceOption { Value = "Active", DisplayOrder = 0 },
                new ChoiceOption { Value = "Archived", DisplayOrder = 2 }
            ]
        };
        var preset = new Preset
        {
            Name = "P",
            Fields = [new DisplayNameFieldDefinition { IsRequired = false }, def]
        };
        await PresetUseCase.CreatePresetAsync(preset);

        var reloaded = (await PresetRepo.GetAllAsync())[0];
        var reloadedDef = reloaded.Fields.OfType<SingleChoiceFieldDefinition>().First();

        var ordered = reloadedDef.Choices.OrderBy(c => c.DisplayOrder).Select(c => c.Value).ToList();
        Assert.That(ordered, Is.EqualTo(new[] { "Active", "Pending", "Archived" }));
    }
}
