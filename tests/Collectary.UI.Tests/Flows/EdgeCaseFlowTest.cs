using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.Flows;

[TestFixture]
public class EdgeCaseFlowTest : FlowTestBase
{
    [Test]
    public async Task SavePreset_EmptyName_SavesWithoutException()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = string.Empty;

        Assert.DoesNotThrowAsync(async () => await sut.BackCommand.ExecuteAsync(null));
    }

    [Test]
    public async Task SaveItem_TrimmedDisplayName()
    {
        var preset = new Preset
        {
            Name = "P",
            Fields = [new DisplayNameFieldDefinition { IsRequired = false }]
        };
        await PresetUseCase.CreatePresetAsync(preset);
        var ef = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);

        var vm = MakeItemEditorVm(preset, ef);
        SetDisplayName(vm, "  hello  ");
        await vm.PersistAsync();

        var items = await ItemUseCase.GetItemsForPresetAsync(preset.Id);
        Assert.That(items[0].DisplayName, Is.EqualTo("hello"));
    }

    [Test]
    public async Task PresetWithNoFields_CanStillCreateItem()
    {
        var preset = new Preset { Name = "Empty", Fields = [] };
        await PresetUseCase.CreatePresetAsync(preset);
        var ef = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);

        var vm = MakeItemEditorVm(preset, ef);
        SetDisplayName(vm, "EmptyItem");

        Assert.DoesNotThrowAsync(async () => await vm.PersistAsync());

        var items = await ItemUseCase.GetItemsForPresetAsync(preset.Id);
        Assert.That(items, Has.Count.EqualTo(1));
    }

    [Test]
    public void ColorField_DefaultFormat_IsHex()
    {
        var def = new ColorFieldDefinition();
        Assert.That(def.Format, Is.EqualTo(ColorFormat.Hex));
    }

    [Test]
    public void RatingField_DefaultMaxStars_IsFive()
    {
        var def = new RatingFieldDefinition();
        Assert.That(def.MaxStars, Is.EqualTo(5));
    }

    [Test]
    public void CurrencyField_DefaultSymbol_IsEuro()
    {
        var def = new CurrencyFieldDefinition();
        Assert.That(def.CurrencySymbol, Is.EqualTo("€"));
    }

    [Test]
    public void ImageField_DefaultSizeMode_IsFixed()
    {
        var def = new ImageFieldDefinition();
        Assert.That(def.SizeMode, Is.EqualTo(ImageSizeMode.Fixed));
    }

    [Test]
    public async Task ReloadPreset_AfterAddingListSubField_SubFieldPresent()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        await sut.AddFieldAsync<ListFieldDefinition>();
        var listRow = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => f.IsList);
        listRow.Label = "Chapters";

        sut.DrillIntoCommand.Execute(listRow);
        await sut.AddFieldAsync<TextFieldDefinition>();
        sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last().Label = "ChapterName";

        await sut.BackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        var listDef = saved.Fields.OfType<ListFieldDefinition>().First();
        Assert.That(listDef.SubFields, Has.Count.EqualTo(1));
        Assert.That(listDef.SubFields[0].Label, Is.EqualTo("ChapterName"));
    }

    [Test]
    public async Task ListField_ZeroEntries_EntryCountIsZero()
    {
        var preset = new Preset
        {
            Name = "P",
            Fields =
            [
                new DisplayNameFieldDefinition { IsRequired = false },
                new ListFieldDefinition
                {
                    Label = "List",
                    SubFields = [new TextFieldDefinition { Label = "X", DisplayOrder = 0 }]
                }
            ]
        };
        foreach (var sub in preset.Fields.OfType<ListFieldDefinition>().SelectMany(l => l.SubFields))
            sub.ParentListFieldDefinitionId = preset.Fields.OfType<ListFieldDefinition>().First().Id;

        await PresetUseCase.CreatePresetAsync(preset);
        var ef = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);

        var vm = MakeItemEditorVm(preset, ef);
        var list = vm.FieldEditors.OfType<ListFieldEditorViewModel>().Single();

        Assert.That(list.EntryCount, Is.EqualTo(0));
    }

    [Test]
    public async Task EditPreset_AddingNewField_NewFieldPersistedOnUpdate()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        await sut.BackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        var editor = MakePresetEditorVm(existing: saved);
        await editor.LoadAsync();
        await editor.AddFieldAsync<TextFieldDefinition>();
        editor.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => !f.IsDisplayName).Label = "NewField";
        await editor.BackCommand.ExecuteAsync(null);

        var reloaded = (await PresetRepo.GetAllAsync())[0];
        Assert.That(reloaded.Fields.Any(f => f.Label == "NewField"), Is.True);
    }

    [Test]
    public async Task EditPreset_RemovingField_FieldGoneAfterUpdate()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        await sut.AddFieldAsync<TextFieldDefinition>();
        var fieldRow = sut.CurrentRows.OfType<FieldDefinitionRowViewModel>().Last(f => !f.IsDisplayName);
        fieldRow.Label = "ToRemove";
        await sut.BackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        var editor = MakePresetEditorVm(existing: saved);
        await editor.LoadAsync();
        var toRemove = editor.CurrentRows.OfType<FieldDefinitionRowViewModel>()
            .First(f => f.Label == "ToRemove");
        await editor.RemoveFieldCommand.ExecuteAsync(toRemove);
        await editor.BackCommand.ExecuteAsync(null);

        var reloaded = (await PresetRepo.GetAllAsync())[0];
        Assert.That(reloaded.Fields.Any(f => f.Label == "ToRemove"), Is.False);
    }

    [Test]
    public async Task MultipleItems_AllReturnedByGetItemsForPreset()
    {
        var preset = new Preset
        {
            Name = "P",
            Fields = [new DisplayNameFieldDefinition { IsRequired = false }]
        };
        await PresetUseCase.CreatePresetAsync(preset);
        var ef = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);

        for (var i = 1; i <= 5; i++)
        {
            var vm = MakeItemEditorVm(preset, ef);
            SetDisplayName(vm, $"Item {i}");
            await vm.PersistAsync();
        }

        var items = await ItemUseCase.GetItemsForPresetAsync(preset.Id);
        Assert.That(items, Has.Count.EqualTo(5));
    }
}
