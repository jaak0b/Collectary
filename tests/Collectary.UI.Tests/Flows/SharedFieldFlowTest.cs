using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.SharedFields;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.Flows;

[TestFixture]
public class SharedFieldFlowTest : FlowTestBase
{
    private SharedFieldLibraryViewModel MakeLibraryVm(Action? onDone = null) =>
        new(SharedFieldUseCase, A.Fake<IDialogService>(), Mapper, onDone ?? (() => { }));

    [Test]
    public async Task CreateSharedField_AppearsInLibrary()
    {
        var sf = new SharedField
        {
            Name = "ISBN",
            Definition = new TextFieldDefinition { Label = "ISBN" }
        };
        sf.Definition.SharedFieldId = sf.Id;
        await SharedFieldUseCase.CreateAsync(sf);

        var vm = MakeLibraryVm();
        await vm.LoadAsync();

        Assert.That(vm.CurrentRows, Has.Count.EqualTo(1));
        Assert.That(((FieldDefinitionRowViewModel)vm.CurrentRows[0]).Label, Is.EqualTo("ISBN"));
    }

    [Test]
    public async Task EditSharedField_UpdatesName()
    {
        var sf = new SharedField
        {
            Name = "OldName",
            Definition = new TextFieldDefinition { Label = "OldName" }
        };
        sf.Definition.SharedFieldId = sf.Id;
        await SharedFieldUseCase.CreateAsync(sf);

        var vm = MakeLibraryVm();
        await vm.LoadAsync();
        var row = (FieldDefinitionRowViewModel)vm.CurrentRows[0];
        row.Label = "NewName";
        await vm.SaveCommand.ExecuteAsync(null);

        var all = await SharedFieldUseCase.GetAllAsync();
        Assert.That(all[0].Name, Is.EqualTo("NewName"),
            "Saving the library should persist the updated system field name");
    }

    [Test]
    public async Task DeleteSharedField_RemovesFromLibrary()
    {
        var sf = new SharedField
        {
            Name = "ToDelete",
            Definition = new TextFieldDefinition { Label = "ToDelete" }
        };
        sf.Definition.SharedFieldId = sf.Id;
        await SharedFieldUseCase.CreateAsync(sf);

        await SharedFieldUseCase.DeleteAsync(sf.Id);

        var all = await SharedFieldUseCase.GetAllAsync();
        Assert.That(all, Is.Empty);
    }

    [Test]
    public async Task AddSharedFieldToPreset_AppearsInSharedFieldRefs()
    {
        var sf = new SharedField
        {
            Name = "Rating",
            Definition = new RatingFieldDefinition { Label = "Rating" }
        };
        sf.Definition.SharedFieldId = sf.Id;
        await SharedFieldUseCase.CreateAsync(sf);

        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "Books";
        sut.AddSharedFieldCommand.Execute(new SharedFieldRowViewModel(sf));
        await sut.BackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        Assert.That(saved.SharedFieldRefs, Has.Count.EqualTo(1));
        Assert.That(saved.SharedFieldRefs[0].SharedFieldId, Is.EqualTo(sf.Id));
    }

    [Test]
    public async Task AddSharedFieldTwice_NoDuplicate()
    {
        var sf = new SharedField
        {
            Name = "Tag",
            Definition = new TextFieldDefinition { Label = "Tag" }
        };
        sf.Definition.SharedFieldId = sf.Id;
        await SharedFieldUseCase.CreateAsync(sf);

        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        var row = new SharedFieldRowViewModel(sf);
        sut.AddSharedFieldCommand.Execute(row);
        sut.AddSharedFieldCommand.Execute(row);
        await sut.BackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        Assert.That(saved.SharedFieldRefs, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AddSharedField_NotAddedToOwnFields()
    {
        var sf = new SharedField
        {
            Name = "Series",
            Definition = new TextFieldDefinition { Label = "Series" }
        };
        sf.Definition.SharedFieldId = sf.Id;
        await SharedFieldUseCase.CreateAsync(sf);

        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        sut.AddSharedFieldCommand.Execute(new SharedFieldRowViewModel(sf));
        await sut.BackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        Assert.That(saved.Fields.Any(f => f.SharedFieldId == sf.Id), Is.False,
            "A system field reference must be stored in SharedFieldRefs, not in Fields");
    }

    [Test]
    public async Task SharedField_EffectiveFields_IncludesSharedFieldDefinition()
    {
        var sfDef = new TextFieldDefinition { Label = "Publisher" };
        var sf = new SharedField { Name = "Publisher", Definition = sfDef };
        sfDef.SharedFieldId = sf.Id;
        await SharedFieldUseCase.CreateAsync(sf);

        var preset = new Preset
        {
            Name = "Books",
            Fields = [new DisplayNameFieldDefinition { IsRequired = false }],
            SharedFieldRefs = [new PresetSharedField { SharedFieldId = sf.Id, DisplayOrder = 1 }]
        };
        await PresetUseCase.CreatePresetAsync(preset);

        var ef = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);
        Assert.That(ef.Fields.Any(f => f.Label == "Publisher"), Is.True,
            "GetEffectiveFieldsAsync must include system field definitions");
    }

    [Test]
    public async Task ReorderSharedFields_PersistsNewOrder()
    {
        var sfA = new SharedField { Name = "A", Definition = new TextFieldDefinition { Label = "A" } };
        sfA.Definition.SharedFieldId = sfA.Id;
        var sfB = new SharedField { Name = "B", Definition = new TextFieldDefinition { Label = "B" } };
        sfB.Definition.SharedFieldId = sfB.Id;
        var sfC = new SharedField { Name = "C", Definition = new TextFieldDefinition { Label = "C" } };
        sfC.Definition.SharedFieldId = sfC.Id;

        await SharedFieldUseCase.CreateAsync(sfA);
        await SharedFieldUseCase.CreateAsync(sfB);
        await SharedFieldUseCase.CreateAsync(sfC);

        await SharedFieldUseCase.ReorderAsync([sfC.Id, sfA.Id, sfB.Id]);

        var all = await SharedFieldUseCase.GetAllAsync();
        Assert.That(all.Select(sf => sf.Name), Is.EqualTo(new[] { "C", "A", "B" }));
    }
}
