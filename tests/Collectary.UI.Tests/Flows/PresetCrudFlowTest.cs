using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.Flows;

[TestFixture]
public class PresetCrudFlowTest : FlowTestBase
{
    [Test]
    public async Task CreatePreset_AppearsInHomeViewModel()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "My Library";

        await sut.BackCommand.ExecuteAsync(null);

        var home = new HomeViewModel(PresetUseCase, ItemUseCase, A.Fake<IDialogService>());
        await home.LoadAsync();
        Assert.That(home.Rows, Has.Count.EqualTo(1));
        Assert.That(home.Rows[0].Preset.Name, Is.EqualTo("My Library"));
    }

    [Test]
    public async Task EditPreset_UpdatesName()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "Original";
        await sut.BackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        var editor = MakePresetEditorVm(existing: saved);
        await editor.LoadAsync();
        editor.Name = "Updated";
        await editor.BackCommand.ExecuteAsync(null);

        var reloaded = (await PresetRepo.GetAllAsync())[0];
        Assert.That(reloaded.Name, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task DeletePreset_RemovesFromList()
    {
        var preset = new Preset { Name = "ToDelete" };
        await PresetUseCase.CreatePresetAsync(preset);

        await PresetUseCase.DeletePresetAsync(preset.Id);

        var all = await PresetRepo.GetAllAsync();
        Assert.That(all, Is.Empty);
    }

    [Test]
    public async Task DeletePreset_CascadesItems()
    {
        var preset = new Preset { Name = "P" };
        await PresetUseCase.CreatePresetAsync(preset);

        var item1 = new Item { PresetId = preset.Id, DisplayName = "A" };
        var item2 = new Item { PresetId = preset.Id, DisplayName = "B" };
        await ItemUseCase.CreateItemAsync(item1);
        await ItemUseCase.CreateItemAsync(item2);

        await PresetUseCase.DeletePresetAsync(preset.Id);

        var items = await ItemRepo.GetByPresetAsync(preset.Id);
        Assert.That(items, Is.Empty);
    }

    [Test]
    public async Task CreatePresetWithParent_SetsParentPresetId()
    {
        var parent = new Preset { Name = "Parent" };
        await PresetUseCase.CreatePresetAsync(parent);

        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "Child";
        sut.SelectedParent = parent;
        await sut.BackCommand.ExecuteAsync(null);

        var all = await PresetRepo.GetAllAsync();
        var child = all.First(p => p.Name == "Child");
        Assert.That(child.ParentPresetId, Is.EqualTo(parent.Id));
    }

    [Test]
    public async Task ReorderPresets_PersistsNewOrder()
    {
        var a = new Preset { Name = "A", DisplayOrder = 0 };
        var b = new Preset { Name = "B", DisplayOrder = 1 };
        var c = new Preset { Name = "C", DisplayOrder = 2 };
        await PresetUseCase.CreatePresetAsync(a);
        await PresetUseCase.CreatePresetAsync(b);
        await PresetUseCase.CreatePresetAsync(c);

        await PresetUseCase.UpdatePresetOrderAsync(new[] { c, a, b });

        var reloaded = await PresetRepo.GetAllAsync();
        Assert.That(reloaded.Select(p => p.Name), Is.EqualTo(new[] { "C", "A", "B" }));
    }

    [Test]
    public async Task CreatePreset_DefaultColumnCountIsOne()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        await sut.BackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        Assert.That(saved.ColumnCount, Is.EqualTo(1));
    }

    [Test]
    public async Task CreatePreset_ColumnCount_RoundTrips()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        sut.ColumnCount = 3;
        await sut.BackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        Assert.That(saved.ColumnCount, Is.EqualTo(3));
    }

    [Test]
    public async Task CreatePreset_PresetEditorVm_HasDisplayNameFieldAutomatically()
    {
        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        await sut.BackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        Assert.That(saved.Fields.Any(f => f is DisplayNameFieldDefinition), Is.True,
            "A saved preset must contain a DisplayNameFieldDefinition");
    }
}
