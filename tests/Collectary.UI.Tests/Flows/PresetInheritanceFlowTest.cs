using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.Flows;

[TestFixture]
public class PresetInheritanceFlowTest : FlowTestBase
{
    [Test]
    public async Task Child_EffectiveFields_IncludesParentFields()
    {
        var parent = new Preset
        {
            Name = "Parent",
            Fields =
            [
                new DisplayNameFieldDefinition { IsRequired = false },
                new TextFieldDefinition { Label = "Genre", DisplayOrder = 1 }
            ]
        };
        await PresetUseCase.CreatePresetAsync(parent);

        var child = new Preset
        {
            Name = "Child",
            ParentPresetId = parent.Id,
            Fields = [new TextFieldDefinition { Label = "ISBN", DisplayOrder = 0 }]
        };
        await PresetUseCase.CreatePresetAsync(child);

        var ef = await PresetUseCase.GetEffectiveFieldsAsync(child.Id);
        Assert.That(ef.Fields.Any(f => f.Label == "Genre"), Is.True,
            "Child preset effective fields must include parent's own fields");
        Assert.That(ef.Fields.Any(f => f.Label == "ISBN"), Is.True,
            "Child preset effective fields must include its own fields");
    }

    [Test]
    public async Task Child_EffectiveFields_ParentDisplayName_NotDuplicated()
    {
        var parent = new Preset
        {
            Name = "Parent",
            Fields = [new DisplayNameFieldDefinition { IsRequired = true }]
        };
        await PresetUseCase.CreatePresetAsync(parent);

        var child = new Preset
        {
            Name = "Child",
            ParentPresetId = parent.Id,
            Fields = [new DisplayNameFieldDefinition { IsRequired = false }]
        };
        await PresetUseCase.CreatePresetAsync(child);

        var ef = await PresetUseCase.GetEffectiveFieldsAsync(child.Id);
        var titleFields = ef.Fields.Where(f => f.IsTitleField).ToList();
        Assert.That(titleFields, Has.Count.EqualTo(1),
            "Effective fields must contain exactly one title field even when parent and child both define one");
    }

    [Test]
    public async Task Child_OwnFields_AppendedAfterParentFields()
    {
        var parent = new Preset
        {
            Name = "Parent",
            Fields =
            [
                new DisplayNameFieldDefinition { IsRequired = false, DisplayOrder = 0 },
                new TextFieldDefinition { Label = "A", DisplayOrder = 1 },
                new TextFieldDefinition { Label = "B", DisplayOrder = 2 }
            ]
        };
        await PresetUseCase.CreatePresetAsync(parent);

        var child = new Preset
        {
            Name = "Child",
            ParentPresetId = parent.Id,
            Fields = [new TextFieldDefinition { Label = "C", DisplayOrder = 0 }]
        };
        await PresetUseCase.CreatePresetAsync(child);

        var ef = await PresetUseCase.GetEffectiveFieldsAsync(child.Id);
        var nonTitle = ef.Fields.Where(f => !f.IsTitleField).ToList();
        var aIndex = nonTitle.FindIndex(f => f.Label == "A");
        var bIndex = nonTitle.FindIndex(f => f.Label == "B");
        var cIndex = nonTitle.FindIndex(f => f.Label == "C");

        Assert.That(aIndex, Is.LessThan(cIndex), "Parent field A must come before child field C");
        Assert.That(bIndex, Is.LessThan(cIndex), "Parent field B must come before child field C");
    }

    [Test]
    public async Task GrandChild_InheritsFromAllAncestors()
    {
        var grandParent = new Preset
        {
            Name = "GrandParent",
            Fields =
            [
                new DisplayNameFieldDefinition { IsRequired = false },
                new TextFieldDefinition { Label = "Series", DisplayOrder = 1 }
            ]
        };
        await PresetUseCase.CreatePresetAsync(grandParent);

        var parent = new Preset
        {
            Name = "Parent",
            ParentPresetId = grandParent.Id,
            Fields = [new TextFieldDefinition { Label = "Publisher", DisplayOrder = 0 }]
        };
        await PresetUseCase.CreatePresetAsync(parent);

        var child = new Preset
        {
            Name = "Child",
            ParentPresetId = parent.Id,
            Fields = [new TextFieldDefinition { Label = "Edition", DisplayOrder = 0 }]
        };
        await PresetUseCase.CreatePresetAsync(child);

        var ef = await PresetUseCase.GetEffectiveFieldsAsync(child.Id);
        Assert.That(ef.Fields.Any(f => f.Label == "Series"), Is.True, "grandparent field must be inherited");
        Assert.That(ef.Fields.Any(f => f.Label == "Publisher"), Is.True, "parent field must be inherited");
        Assert.That(ef.Fields.Any(f => f.Label == "Edition"), Is.True, "own field must be present");
    }

    [Test]
    public async Task ChildItem_StoredWithChildPresetId()
    {
        var parent = new Preset
        {
            Name = "Parent",
            Fields = [new DisplayNameFieldDefinition { IsRequired = false }]
        };
        await PresetUseCase.CreatePresetAsync(parent);

        var child = new Preset
        {
            Name = "Child",
            ParentPresetId = parent.Id,
            Fields = []
        };
        await PresetUseCase.CreatePresetAsync(child);

        var ef = await PresetUseCase.GetEffectiveFieldsAsync(child.Id);
        var vm = MakeItemEditorVm(child, ef);
        SetDisplayName(vm, "MyItem");
        await vm.PersistAsync();

        var items = await ItemUseCase.GetItemsForPresetAsync(child.Id);
        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].PresetId, Is.EqualTo(child.Id));
    }

    [Test]
    public async Task DeleteParent_WithChildPreset_IsRestricted()
    {
        var parent = new Preset { Name = "Parent", Fields = [] };
        await PresetUseCase.CreatePresetAsync(parent);

        var child = new Preset
        {
            Name = "Child",
            ParentPresetId = parent.Id,
            Fields = []
        };
        await PresetUseCase.CreatePresetAsync(child);

        var threw = false;
        try { await PresetUseCase.DeletePresetAsync(parent.Id); }
        catch { threw = true; }

        Assert.That(threw, Is.True,
            "Deleting a parent preset that has children should throw a FK constraint violation");
    }
}
