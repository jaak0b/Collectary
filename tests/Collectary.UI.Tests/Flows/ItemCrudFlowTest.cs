using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.Flows;

[TestFixture]
public class ItemCrudFlowTest : FlowTestBase
{
    private Preset _preset = null!;
    private EffectiveFields _effectiveFields = null!;

    [SetUp]
    public async Task SetUpPreset()
    {
        _preset = new Preset
        {
            Name = "Books",
            Fields = [new DisplayNameFieldDefinition { IsRequired = true }]
        };
        await PresetUseCase.CreatePresetAsync(_preset);
        _effectiveFields = await PresetUseCase.GetEffectiveFieldsAsync(_preset.Id);
    }

    [Test]
    public async Task CreateItem_AppearsInPresetDetail()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        SetDisplayName(vm, "Clean Code");
        await vm.PersistAsync();

        var detail = MakePresetDetailVm(_preset);
        await detail.LoadAsync();

        Assert.That(detail.ItemRows, Has.Count.EqualTo(1));
        Assert.That(detail.ItemRows[0].DisplayName, Is.EqualTo("Clean Code"));
    }

    [Test]
    public async Task PresetDetail_PrefillsQueryWithPresetClause()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        SetDisplayName(vm, "Clean Code");
        await vm.PersistAsync();

        var detail = MakePresetDetailVm(_preset);
        await detail.LoadAsync();

        Assert.That(detail.Query.QueryText, Is.EqualTo("preset = \"Books\""));
        Assert.That(detail.ItemRows, Has.Count.EqualTo(1));
        Assert.That(detail.ShowCollectionColumn, Is.False);
    }

    [Test]
    public async Task PresetDetail_QueryAcrossPresets_ShowsCollectionColumn()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        SetDisplayName(vm, "Clean Code");
        await vm.PersistAsync();

        var games = new Preset
        {
            Name = "Games",
            Fields = [new DisplayNameFieldDefinition { IsRequired = true }]
        };
        await PresetUseCase.CreatePresetAsync(games);
        var gameVm = MakeItemEditorVm(games, await PresetUseCase.GetEffectiveFieldsAsync(games.Id));
        SetDisplayName(gameVm, "Catan");
        await gameVm.PersistAsync();

        var detail = MakePresetDetailVm(_preset);
        await detail.LoadAsync();
        detail.Query.QueryText = "ORDER BY name";
        await detail.Query.RunCommand.ExecuteAsync(null);

        Assert.That(detail.ItemRows, Has.Count.EqualTo(2));
        Assert.That(detail.ShowCollectionColumn, Is.True);
        Assert.That(detail.ItemRows.Select(r => r.CollectionName),
            Is.EqualTo(new[] { "Games", "Books" }));
    }

    [Test]
    public async Task PresetDetail_InvalidQuery_ShowsMessageWithoutCrashing()
    {
        var detail = MakePresetDetailVm(_preset);
        await detail.LoadAsync();

        detail.Query.QueryText = "Ghost = 1";
        await detail.Query.RunCommand.ExecuteAsync(null);

        Assert.That(detail.Query.QueryMessage, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task EditItem_UpdatesDisplayName()
    {
        var createVm = MakeItemEditorVm(_preset, _effectiveFields);
        SetDisplayName(createVm, "Original");
        await createVm.PersistAsync();
        var created = (await ItemUseCase.GetItemsForPresetAsync(_preset.Id))[0];

        var editVm = MakeItemEditorVm(_preset, _effectiveFields, existing: created);
        SetDisplayName(editVm, "Updated");
        await editVm.PersistAsync();

        var reloaded = await ItemUseCase.GetItemAsync(created.Id);
        Assert.That(reloaded!.DisplayName, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task DeleteItem_RemovesFromPresetDetail()
    {
        var createVm = MakeItemEditorVm(_preset, _effectiveFields);
        SetDisplayName(createVm, "ToDelete");
        await createVm.PersistAsync();
        var created = (await ItemUseCase.GetItemsForPresetAsync(_preset.Id))[0];

        await ItemUseCase.DeleteItemAsync(created.Id);

        var items = await ItemUseCase.GetItemsForPresetAsync(_preset.Id);
        Assert.That(items, Is.Empty);
    }

    [Test]
    public async Task CreateItem_RequiredDisplayName_Missing_SetsError()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.That(vm.ErrorMessage, Is.Not.Null.And.Not.Empty,
            "An empty DisplayName when the field is required should set an error message");
    }

    [Test]
    public async Task CreateItem_OptionalTextField_Blank_Succeeds()
    {
        var preset = new Preset
        {
            Name = "WithOptional",
            Fields =
            [
                new DisplayNameFieldDefinition { IsRequired = false },
                new TextFieldDefinition { Label = "Notes", IsRequired = false }
            ]
        };
        await PresetUseCase.CreatePresetAsync(preset);
        var ef = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);

        var vm = MakeItemEditorVm(preset, ef);
        SetDisplayName(vm, "Item1");

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.That(vm.ErrorMessage, Is.Null);
        var items = await ItemUseCase.GetItemsForPresetAsync(preset.Id);
        Assert.That(items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CreateItem_RequiredTextField_Missing_SetsError()
    {
        var preset = new Preset
        {
            Name = "WithRequired",
            Fields =
            [
                new DisplayNameFieldDefinition { IsRequired = false },
                new TextFieldDefinition { Label = "ISBN", IsRequired = true }
            ]
        };
        await PresetUseCase.CreatePresetAsync(preset);
        var ef = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);

        var vm = MakeItemEditorVm(preset, ef);
        SetDisplayName(vm, "Book");

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.That(vm.ErrorMessage, Is.Not.Null.And.Not.Empty,
            "Missing required ISBN field should set an error message");
    }

    [Test]
    public async Task CreateItem_SetsCorrectPresetId()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        SetDisplayName(vm, "Item");
        await vm.PersistAsync();

        var items = await ItemUseCase.GetItemsForPresetAsync(_preset.Id);
        Assert.That(items[0].PresetId, Is.EqualTo(_preset.Id));
    }

    [Test]
    public async Task CreateThenUpdateItem_DoesNotDuplicate()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        SetDisplayName(vm, "First");
        await vm.PersistAsync();

        SetDisplayName(vm, "Second");
        await vm.PersistAsync();

        var items = await ItemUseCase.GetItemsForPresetAsync(_preset.Id);
        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].DisplayName, Is.EqualTo("Second"));
    }

    [Test]
    public async Task CreateItem_TrimsDisplayName()
    {
        var vm = MakeItemEditorVm(_preset, _effectiveFields);
        SetDisplayName(vm, "  Trimmed  ");
        await vm.PersistAsync();

        var items = await ItemUseCase.GetItemsForPresetAsync(_preset.Id);
        Assert.That(items[0].DisplayName, Is.EqualTo("Trimmed"));
    }
}
