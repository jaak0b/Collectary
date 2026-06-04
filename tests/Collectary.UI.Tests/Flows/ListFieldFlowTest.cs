using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.DI;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.Flows;

[TestFixture]
public class ListFieldFlowTest : FlowTestBase
{
    private Preset _preset = null!;
    private ListFieldDefinition _listDef = null!;
    private EffectiveFields _effectiveFields = null!;

    [SetUp]
    public async Task SetUpPreset()
    {
        _listDef = new ListFieldDefinition
        {
            Label = "Episodes",
            InlineStyle = ListInlineStyle.Card,
            SubFields =
            [
                new TextFieldDefinition { Label = "Title", DisplayOrder = 0, ShowInList = true },
                new IntegerFieldDefinition { Label = "Number", DisplayOrder = 1, ShowInList = true }
            ]
        };
        foreach (var sub in _listDef.SubFields)
            sub.ParentListFieldDefinitionId = _listDef.Id;

        _preset = new Preset
        {
            Name = "Show",
            Fields = [new DisplayNameFieldDefinition { IsRequired = false }, _listDef]
        };
        await PresetUseCase.CreatePresetAsync(_preset);
        _effectiveFields = await PresetUseCase.GetEffectiveFieldsAsync(_preset.Id);
    }

    private ListFieldEditorViewModel GetListEditor(ItemEditorViewModel vm) =>
        vm.FieldEditors.OfType<ListFieldEditorViewModel>().Single();

    [Test]
    public void AddEntry_IncrementsEntryCount()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        var list = GetListEditor(vm);
        Assert.That(list.EntryCount, Is.EqualTo(0));

        list.AddEntryCommand.Execute(null);

        Assert.That(list.EntryCount, Is.EqualTo(1));
    }

    [Test]
    public void AddTwoEntries_EntryCountIsTwo()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        var list = GetListEditor(vm);

        list.AddEntryCommand.Execute(null);
        list.AddEntryCommand.Execute(null);

        Assert.That(list.EntryCount, Is.EqualTo(2));
    }

    [Test]
    public void AddEntry_CreatesSubFieldEditors()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        var list = GetListEditor(vm);

        list.AddEntryCommand.Execute(null);

        var entry = list.Entries[0];
        Assert.That(entry.FieldEditors, Has.Count.EqualTo(2));
        Assert.That(entry.FieldEditors[0].Definition.Label, Is.EqualTo("Title"));
        Assert.That(entry.FieldEditors[1].Definition.Label, Is.EqualTo("Number"));
    }

    [Test]
    public async Task EditEntryValue_Saved_RoundTrips()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        SetDisplayName(vm, "S1");
        var list = GetListEditor(vm);
        list.AddEntryCommand.Execute(null);

        var textEditor = (TextFieldEditorViewModel)list.Entries[0].FieldEditors[0];
        textEditor.Text = "Episode 1";
        await vm.PersistAsync();

        var saved = (await ItemUseCase.GetItemsForPresetAsync(_preset.Id))[0];
        var vm2 = MakeItemEditorVm(_preset, _effectiveFields, existing: saved);
        var list2 = GetListEditor(vm2);

        Assert.That(list2.EntryCount, Is.EqualTo(1));
        var textEditor2 = (TextFieldEditorViewModel)list2.Entries[0].FieldEditors[0];
        Assert.That(textEditor2.Text, Is.EqualTo("Episode 1"));
    }

    [Test]
    public void DeleteEntry_DecrementsEntryCount()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        var list = GetListEditor(vm);

        list.AddEntryCommand.Execute(null);
        list.AddEntryCommand.Execute(null);
        Assert.That(list.EntryCount, Is.EqualTo(2));

        list.DeleteEntryCommand.Execute(list.EntryRows[0]);

        Assert.That(list.EntryCount, Is.EqualTo(1));
    }

    [Test]
    public async Task DeleteEntry_RemainingEntry_ValuePreserved()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        SetDisplayName(vm, "S");
        var list = GetListEditor(vm);
        list.AddEntryCommand.Execute(null);
        list.AddEntryCommand.Execute(null);

        ((TextFieldEditorViewModel)list.Entries[1].FieldEditors[0]).Text = "Keep Me";
        list.DeleteEntryCommand.Execute(list.EntryRows[0]);
        await vm.PersistAsync();

        var saved = (await ItemUseCase.GetItemsForPresetAsync(_preset.Id))[0];
        var vm2 = MakeItemEditorVm(_preset, _effectiveFields, existing: saved);
        var list2 = GetListEditor(vm2);

        Assert.That(list2.EntryCount, Is.EqualTo(1));
        var text = (TextFieldEditorViewModel)list2.Entries[0].FieldEditors[0];
        Assert.That(text.Text, Is.EqualTo("Keep Me"));
    }

    [Test]
    public void RenumberEntries_AfterDelete_OrderIsContiguous()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        var list = GetListEditor(vm);

        list.AddEntryCommand.Execute(null);
        list.AddEntryCommand.Execute(null);
        list.AddEntryCommand.Execute(null);

        list.DeleteEntryCommand.Execute(list.EntryRows[1]);

        Assert.That(list.Entries[0].EntryNumber, Is.EqualTo(1));
        Assert.That(list.Entries[1].EntryNumber, Is.EqualTo(2));
    }

    [Test]
    public async Task AddEntries_SaveAndReload_EntryCountPreserved()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        SetDisplayName(vm, "Show1");
        var list = GetListEditor(vm);
        list.AddEntryCommand.Execute(null);
        list.AddEntryCommand.Execute(null);
        list.AddEntryCommand.Execute(null);
        await vm.PersistAsync();

        var saved = (await ItemUseCase.GetItemsForPresetAsync(_preset.Id))[0];
        var vm2 = MakeItemEditorVm(_preset, _effectiveFields, existing: saved);
        var list2 = GetListEditor(vm2);

        Assert.That(list2.EntryCount, Is.EqualTo(3));
    }

    [Test]
    public void ListField_CardInlineStyle_IsCardInline()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        var list = GetListEditor(vm);

        Assert.That(list.IsCardInline, Is.True);
        Assert.That(list.IsGridInline, Is.False);
    }

    [Test]
    public async Task ListField_GridInlineStyle_IsGridInline()
    {
        var gridPreset = new Preset
        {
            Name = "Grid",
            Fields =
            [
                new DisplayNameFieldDefinition { IsRequired = false },
                new ListFieldDefinition
                {
                    Label = "List",
                    InlineStyle = ListInlineStyle.Grid,
                    SubFields = [new TextFieldDefinition { Label = "Name", ShowInList = true, DisplayOrder = 0 }]
                }
            ]
        };
        foreach (var sub in gridPreset.Fields.OfType<ListFieldDefinition>().SelectMany(l => l.SubFields))
            sub.ParentListFieldDefinitionId = gridPreset.Fields.OfType<ListFieldDefinition>().First().Id;

        await PresetUseCase.CreatePresetAsync(gridPreset);
        var ef = await PresetUseCase.GetEffectiveFieldsAsync(gridPreset.Id);

        A.CallTo(() => CellBuilder.HasListCellViewModel(A<Type>._)).Returns(true);
        var vm = MakeItemEditorVm(gridPreset, ef);
        var list = GetListEditor(vm);

        Assert.That(list.IsGridInline, Is.True);
        Assert.That(list.IsCardInline, Is.False);
    }

    [Test]
    public async Task ListField_EmptySubFields_EntryHasNoEditors()
    {
        var emptyListPreset = new Preset
        {
            Name = "Empty",
            Fields =
            [
                new DisplayNameFieldDefinition { IsRequired = false },
                new ListFieldDefinition { Label = "Notes" }
            ]
        };
        await PresetUseCase.CreatePresetAsync(emptyListPreset);
        var ef = await PresetUseCase.GetEffectiveFieldsAsync(emptyListPreset.Id);

        var vm = MakeItemEditorVm(emptyListPreset, ef);
        var list = vm.FieldEditors.OfType<ListFieldEditorViewModel>().Single();
        list.AddEntryCommand.Execute(null);

        Assert.That(list.Entries[0].FieldEditors, Is.Empty);
    }
}
