using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.ItemEditor;

[TestFixture]
public class GroupLayoutFlowTest : FlowTestBase
{
    private static Preset MakePreset(string name, IReadOnlyList<FieldDefinition> fields,
        IReadOnlyList<FieldGroup>? groups = null) =>
        new()
        {
            Name = name,
            Fields = fields.ToList(),
            Groups = groups?.ToList() ?? []
        };

    private ItemEditorViewModel MakeVm(Preset preset, EffectiveFields ef, Item? existing = null) =>
        MakeItemEditorVm(preset, ef, existing);

    private static EffectiveFields MakeEf(IReadOnlyList<FieldDefinition> fields,
        IReadOnlyList<FieldGroup>? groups = null,
        IDictionary<Guid, Guid?>? groupByFieldId = null) =>
        new()
        {
            Fields = fields,
            Groups = groups ?? [],
            GroupByFieldId = (IReadOnlyDictionary<Guid, Guid?>?)groupByFieldId ?? new Dictionary<Guid, Guid?>()
        };

    [Test]
    public async Task CardGroup_FieldInGroup_AppearsInGroupEditors()
    {
        var group = new FieldGroup { Name = "Details", DisplayMode = GroupDisplayMode.Card, DisplayOrder = 0 };
        var field = new TextFieldDefinition { Label = "Genre" };
        var preset = MakePreset("P", [field], [group]);
        await PresetUseCase.CreatePresetAsync(preset);

        var ef = MakeEf([field], [group], new Dictionary<Guid, Guid?> { [field.Id] = group.Id });
        var vm = MakeVm(preset, ef);

        var card = vm.LayoutRegions.OfType<FieldGroupViewModel>().Single();
        Assert.That(card.Name, Is.EqualTo("Details"));
        Assert.That(card.Editors, Has.Count.EqualTo(1));
        Assert.That(vm.UngroupedEditors, Is.Empty);
    }

    [Test]
    public async Task TabGroup_FieldsFromTwoTabs_MergeIntoTabRegion()
    {
        var tab1 = new FieldGroup { Name = "T1", DisplayMode = GroupDisplayMode.Tab, DisplayOrder = 0 };
        var tab2 = new FieldGroup { Name = "T2", DisplayMode = GroupDisplayMode.Tab, DisplayOrder = 1 };
        var f1 = new TextFieldDefinition { Label = "F1" };
        var f2 = new TextFieldDefinition { Label = "F2" };
        var preset = MakePreset("P", [f1, f2], [tab1, tab2]);
        await PresetUseCase.CreatePresetAsync(preset);

        var ef = MakeEf([f1, f2], [tab1, tab2],
            new Dictionary<Guid, Guid?> { [f1.Id] = tab1.Id, [f2.Id] = tab2.Id });
        var vm = MakeVm(preset, ef);

        var tabRegion = vm.LayoutRegions.OfType<TabRegionViewModel>().Single();
        Assert.That(tabRegion.TabGroups, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task DefaultCollapsed_True_StartsCollapsed()
    {
        var group = new FieldGroup
        {
            Name = "G", DisplayMode = GroupDisplayMode.Card, DefaultCollapsed = true, DisplayOrder = 0
        };
        var field = new TextFieldDefinition { Label = "F" };
        var preset = MakePreset("P", [field], [group]);
        await PresetUseCase.CreatePresetAsync(preset);

        var ef = MakeEf([field], [group], new Dictionary<Guid, Guid?> { [field.Id] = group.Id });
        var vm = MakeVm(preset, ef);

        var card = vm.LayoutRegions.OfType<FieldGroupViewModel>().Single();
        Assert.That(card.IsExpanded, Is.False);
    }

    [Test]
    public async Task DefaultCollapsed_False_StartsExpanded()
    {
        var group = new FieldGroup
        {
            Name = "G", DisplayMode = GroupDisplayMode.Card, DefaultCollapsed = false, DisplayOrder = 0
        };
        var field = new TextFieldDefinition { Label = "F" };
        var preset = MakePreset("P", [field], [group]);
        await PresetUseCase.CreatePresetAsync(preset);

        var ef = MakeEf([field], [group], new Dictionary<Guid, Guid?> { [field.Id] = group.Id });
        var vm = MakeVm(preset, ef);

        var card = vm.LayoutRegions.OfType<FieldGroupViewModel>().Single();
        Assert.That(card.IsExpanded, Is.True);
    }

    [Test]
    public async Task ToggleExpanded_CardGroup_ChangesState()
    {
        var group = new FieldGroup
        {
            Name = "G", DisplayMode = GroupDisplayMode.Card, DefaultCollapsed = false, DisplayOrder = 0
        };
        var field = new TextFieldDefinition { Label = "F" };
        var preset = MakePreset("P", [field], [group]);
        await PresetUseCase.CreatePresetAsync(preset);

        var ef = MakeEf([field], [group], new Dictionary<Guid, Guid?> { [field.Id] = group.Id });
        var vm = MakeVm(preset, ef);
        var card = vm.LayoutRegions.OfType<FieldGroupViewModel>().Single();

        card.IsExpanded = false;
        Assert.That(card.IsExpanded, Is.False);

        card.IsExpanded = true;
        Assert.That(card.IsExpanded, Is.True);
    }

    [Test]
    public async Task NestedCardGroup_AppearsAsChildRegion()
    {
        var parent = new FieldGroup { Name = "Parent", DisplayMode = GroupDisplayMode.Card, DisplayOrder = 0 };
        var child = new FieldGroup
        {
            Name = "Child", DisplayMode = GroupDisplayMode.Card, DisplayOrder = 0, ParentGroupId = parent.Id
        };
        var field = new TextFieldDefinition { Label = "Deep" };
        var preset = MakePreset("P", [field], [parent, child]);
        await PresetUseCase.CreatePresetAsync(preset);

        var ef = MakeEf([field], [parent, child], new Dictionary<Guid, Guid?> { [field.Id] = child.Id });
        var vm = MakeVm(preset, ef);

        var parentCard = vm.LayoutRegions.OfType<FieldGroupViewModel>().Single();
        Assert.That(parentCard.Name, Is.EqualTo("Parent"));
        var childCard = parentCard.ChildRegions.OfType<FieldGroupViewModel>().Single();
        Assert.That(childCard.Name, Is.EqualTo("Child"));
        Assert.That(childCard.Editors, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task PersistAsync_CollectsValuesFromGroupedFields()
    {
        var group = new FieldGroup { Name = "G", DisplayMode = GroupDisplayMode.Card, DisplayOrder = 0 };
        var grouped = new TextFieldDefinition { Label = "A" };
        var ungrouped = new TextFieldDefinition { Label = "B" };

        var preset = new Preset
        {
            Name = "P",
            Fields = [new DisplayNameFieldDefinition { IsRequired = false }, grouped, ungrouped],
            Groups = [group]
        };
        await PresetUseCase.CreatePresetAsync(preset);

        var ef = MakeEf([grouped, ungrouped], [group],
            new Dictionary<Guid, Guid?> { [grouped.Id] = group.Id, [ungrouped.Id] = null });
        var vm = MakeVm(preset, ef);
        SetDisplayName(vm, "Item1");

        var card = vm.LayoutRegions.OfType<FieldGroupViewModel>().Single();
        var aEditor = (TextFieldEditorViewModel)card.Editors[0];
        aEditor.Text = "GroupedValue";

        var bEditor = (TextFieldEditorViewModel)vm.UngroupedEditors[0];
        bEditor.Text = "UngroupedValue";

        await vm.PersistAsync();

        var saved = (await ItemUseCase.GetItemsForPresetAsync(preset.Id))[0];
        Assert.That(saved.Values, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task MultiColumn_UngroupedFields_UsePresetColumnCount()
    {
        var f1 = new TextFieldDefinition { Label = "A" };
        var f2 = new TextFieldDefinition { Label = "B" };
        var preset = new Preset { Name = "P", ColumnCount = 2, Fields = [f1, f2] };
        await PresetUseCase.CreatePresetAsync(preset);

        var ef = MakeEf([f1, f2]);
        var vm = MakeVm(preset, ef);

        Assert.That(vm.UngroupedColumnCount, Is.EqualTo(2));
    }

    [Test]
    public async Task EmptyCardGroup_ProducesNoRegion()
    {
        var group = new FieldGroup { Name = "Empty", DisplayMode = GroupDisplayMode.Card, DisplayOrder = 0 };
        var field = new TextFieldDefinition { Label = "F" };
        var preset = MakePreset("P", [field], [group]);
        await PresetUseCase.CreatePresetAsync(preset);

        var ef = MakeEf([field], [group], new Dictionary<Guid, Guid?> { [field.Id] = null });
        var vm = MakeVm(preset, ef);

        Assert.That(vm.LayoutRegions.OfType<FieldGroupViewModel>(), Is.Empty,
            "A group with no fields assigned to it should not produce a layout region");
    }
}
