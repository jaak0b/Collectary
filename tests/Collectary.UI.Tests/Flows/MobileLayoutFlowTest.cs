using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.Flows;

[TestFixture]
public class MobileLayoutFlowTest : FlowTestBase
{
    [Test]
    public async Task NarrowMode_SelectField_ShowsDetailHidesList()
    {
        var vm = MakePresetEditorVm();
        await vm.LoadAsync();
        vm.IsNarrow = true;
        vm.AddField<TextFieldDefinition>();

        vm.SelectedNode = vm.CurrentRows[0];

        Assert.That(vm.IsDetailPanelVisible, Is.True);
        Assert.That(vm.IsMasterPanelVisible, Is.False);
    }

    [Test]
    public async Task NarrowMode_Back_ClosesDetailShowsList()
    {
        var vm = MakePresetEditorVm();
        await vm.LoadAsync();
        vm.IsNarrow = true;
        vm.AddField<TextFieldDefinition>();
        vm.SelectedNode = vm.CurrentRows[0];

        await vm.BackCommand.ExecuteAsync(null);

        Assert.That(vm.IsMasterPanelVisible, Is.True);
        Assert.That(vm.IsDetailPanelVisible, Is.False);
    }

    [Test]
    public async Task WideMode_SelectField_BothPanelsVisible()
    {
        var vm = MakePresetEditorVm();
        await vm.LoadAsync();
        vm.IsNarrow = false;
        vm.AddField<TextFieldDefinition>();

        vm.SelectedNode = vm.CurrentRows[0];

        Assert.That(vm.IsMasterPanelVisible, Is.True);
        Assert.That(vm.IsDetailPanelVisible, Is.True);
    }

    [Test]
    public async Task WideMode_NoSelection_BothPanelsVisible()
    {
        var vm = MakePresetEditorVm();
        await vm.LoadAsync();
        vm.IsNarrow = false;
        vm.SelectedNode = null;

        Assert.That(vm.IsMasterPanelVisible, Is.True);
        Assert.That(vm.IsDetailPanelVisible, Is.True);
    }

    [Test]
    public async Task NarrowMode_DrillIntoListField_ThenBack_ReturnsToPreviousLevel()
    {
        var vm = MakePresetEditorVm();
        await vm.LoadAsync();
        vm.IsNarrow = true;
        vm.AddField<ListFieldDefinition>();
        var listRow = vm.CurrentRows.OfType<FieldDefinitionRowViewModel>().First(r => r.IsList);

        vm.DrillIntoCommand.Execute(listRow);
        Assert.That(vm.Levels.Count, Is.EqualTo(2));

        await vm.BackCommand.ExecuteAsync(null);

        Assert.That(vm.Levels.Count, Is.EqualTo(1));
        Assert.That(vm.IsMasterPanelVisible, Is.True);
    }

    [Test]
    public async Task NarrowMode_BackFromSubFieldDetail_ReturnsToSubListNotRoot()
    {
        var vm = MakePresetEditorVm();
        await vm.LoadAsync();
        vm.IsNarrow = true;
        vm.AddField<ListFieldDefinition>();
        var listRow = vm.CurrentRows.OfType<FieldDefinitionRowViewModel>().First(r => r.IsList);
        vm.DrillIntoCommand.Execute(listRow);
        vm.AddField<TextFieldDefinition>();
        vm.SelectedNode = vm.CurrentRows[0];

        await vm.BackCommand.ExecuteAsync(null);

        Assert.That(vm.Levels.Count, Is.EqualTo(2));
        Assert.That(vm.IsMasterPanelVisible, Is.True);
        Assert.That(vm.IsDetailPanelVisible, Is.False);

        await vm.BackCommand.ExecuteAsync(null);

        Assert.That(vm.Levels.Count, Is.EqualTo(1));
        Assert.That(vm.IsMasterPanelVisible, Is.True);
    }

    [Test]
    public async Task NarrowMode_BackSavesPreset()
    {
        var vm = MakePresetEditorVm();
        await vm.LoadAsync();
        vm.Name = "MobileTest";
        vm.IsNarrow = true;
        vm.AddField<TextFieldDefinition>();
        vm.SelectedNode = vm.CurrentRows[0];

        await vm.BackCommand.ExecuteAsync(null);

        var presets = await PresetUseCase.GetAllPresetsAsync();
        Assert.That(presets, Has.Count.EqualTo(1));
        Assert.That(presets[0].Name, Is.EqualTo("MobileTest"));
    }
}
