using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.SystemFields;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.Flows;

[TestFixture]
public class SystemFieldFlowTest : FlowTestBase
{
    private SystemFieldLibraryViewModel MakeLibraryVm(Action? onDone = null) =>
        new(SystemFieldUseCase, A.Fake<IDialogService>(), Mapper, onDone ?? (() => { }));

    [Test]
    public async Task CreateSystemField_AppearsInLibrary()
    {
        var sf = new SystemField
        {
            Name = "ISBN",
            Definition = new TextFieldDefinition { Label = "ISBN" }
        };
        sf.Definition.SystemFieldId = sf.Id;
        await SystemFieldUseCase.CreateAsync(sf);

        var vm = MakeLibraryVm();
        await vm.LoadAsync();

        Assert.That(vm.CurrentRows, Has.Count.EqualTo(1));
        Assert.That(((FieldDefinitionRowViewModel)vm.CurrentRows[0]).Label, Is.EqualTo("ISBN"));
    }

    [Test]
    public async Task EditSystemField_UpdatesName()
    {
        var sf = new SystemField
        {
            Name = "OldName",
            Definition = new TextFieldDefinition { Label = "OldName" }
        };
        sf.Definition.SystemFieldId = sf.Id;
        await SystemFieldUseCase.CreateAsync(sf);

        var vm = MakeLibraryVm();
        await vm.LoadAsync();
        var row = (FieldDefinitionRowViewModel)vm.CurrentRows[0];
        row.Label = "NewName";
        await vm.SaveCommand.ExecuteAsync(null);

        var all = await SystemFieldUseCase.GetAllAsync();
        Assert.That(all[0].Name, Is.EqualTo("NewName"),
            "Saving the library should persist the updated system field name");
    }

    [Test]
    public async Task DeleteSystemField_RemovesFromLibrary()
    {
        var sf = new SystemField
        {
            Name = "ToDelete",
            Definition = new TextFieldDefinition { Label = "ToDelete" }
        };
        sf.Definition.SystemFieldId = sf.Id;
        await SystemFieldUseCase.CreateAsync(sf);

        await SystemFieldUseCase.DeleteAsync(sf.Id);

        var all = await SystemFieldUseCase.GetAllAsync();
        Assert.That(all, Is.Empty);
    }

    [Test]
    public async Task AddSystemFieldToPreset_AppearsInSystemFieldRefs()
    {
        var sf = new SystemField
        {
            Name = "Rating",
            Definition = new RatingFieldDefinition { Label = "Rating" }
        };
        sf.Definition.SystemFieldId = sf.Id;
        await SystemFieldUseCase.CreateAsync(sf);

        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "Books";
        sut.AddSystemFieldCommand.Execute(new SystemFieldRowViewModel(sf));
        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        Assert.That(saved.SystemFieldRefs, Has.Count.EqualTo(1));
        Assert.That(saved.SystemFieldRefs[0].SystemFieldId, Is.EqualTo(sf.Id));
    }

    [Test]
    public async Task AddSystemFieldTwice_NoDuplicate()
    {
        var sf = new SystemField
        {
            Name = "Tag",
            Definition = new TextFieldDefinition { Label = "Tag" }
        };
        sf.Definition.SystemFieldId = sf.Id;
        await SystemFieldUseCase.CreateAsync(sf);

        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        var row = new SystemFieldRowViewModel(sf);
        sut.AddSystemFieldCommand.Execute(row);
        sut.AddSystemFieldCommand.Execute(row);
        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        Assert.That(saved.SystemFieldRefs, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AddSystemField_NotAddedToOwnFields()
    {
        var sf = new SystemField
        {
            Name = "Series",
            Definition = new TextFieldDefinition { Label = "Series" }
        };
        sf.Definition.SystemFieldId = sf.Id;
        await SystemFieldUseCase.CreateAsync(sf);

        var sut = MakePresetEditorVm();
        await sut.LoadAsync();
        sut.Name = "P";
        sut.AddSystemFieldCommand.Execute(new SystemFieldRowViewModel(sf));
        await sut.SaveAndGoBackCommand.ExecuteAsync(null);

        var saved = (await PresetRepo.GetAllAsync())[0];
        Assert.That(saved.Fields.Any(f => f.SystemFieldId == sf.Id), Is.False,
            "A system field reference must be stored in SystemFieldRefs, not in Fields");
    }

    [Test]
    public async Task SystemField_EffectiveFields_IncludesSystemFieldDefinition()
    {
        var sfDef = new TextFieldDefinition { Label = "Publisher" };
        var sf = new SystemField { Name = "Publisher", Definition = sfDef };
        sfDef.SystemFieldId = sf.Id;
        await SystemFieldUseCase.CreateAsync(sf);

        var preset = new Preset
        {
            Name = "Books",
            Fields = [new DisplayNameFieldDefinition { IsRequired = false }],
            SystemFieldRefs = [new PresetSystemField { SystemFieldId = sf.Id, DisplayOrder = 1 }]
        };
        await PresetUseCase.CreatePresetAsync(preset);

        var ef = await PresetUseCase.GetEffectiveFieldsAsync(preset.Id);
        Assert.That(ef.Fields.Any(f => f.Label == "Publisher"), Is.True,
            "GetEffectiveFieldsAsync must include system field definitions");
    }

    [Test]
    public async Task ReorderSystemFields_PersistsNewOrder()
    {
        var sfA = new SystemField { Name = "A", Definition = new TextFieldDefinition { Label = "A" } };
        sfA.Definition.SystemFieldId = sfA.Id;
        var sfB = new SystemField { Name = "B", Definition = new TextFieldDefinition { Label = "B" } };
        sfB.Definition.SystemFieldId = sfB.Id;
        var sfC = new SystemField { Name = "C", Definition = new TextFieldDefinition { Label = "C" } };
        sfC.Definition.SystemFieldId = sfC.Id;

        await SystemFieldUseCase.CreateAsync(sfA);
        await SystemFieldUseCase.CreateAsync(sfB);
        await SystemFieldUseCase.CreateAsync(sfC);

        await SystemFieldUseCase.ReorderAsync([sfC.Id, sfA.Id, sfB.Id]);

        var all = await SystemFieldUseCase.GetAllAsync();
        Assert.That(all.Select(sf => sf.Name), Is.EqualTo(new[] { "C", "A", "B" }));
    }
}
