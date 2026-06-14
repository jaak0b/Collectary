using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.ItemEditor;

[TestFixture]
public class ItemEditorFillRandomTest : FlowTestBase
{
    private ItemEditorViewModel BuildVm(IReadOnlyList<FieldDefinition> fields, out ItemEditingContext ctx)
    {
        ctx = MakeItemContext();
        ctx.SampleData = new BogusSampleData(42);
        var vm = new ItemEditorViewModel(
            ItemUseCase,
            PresetUseCase,
            new Preset { ColumnCount = 1 },
            new EffectiveFields { Fields = fields },
            onSaved: () => { },
            onCancelled: () => { },
            context: ctx,
            existing: null);
        ctx.SaveAsync = vm.PersistAsync;
        return vm;
    }

    [Test]
    public void FillRandom_PopulatesNonMediaEditors()
    {
        var vm = BuildVm(
        [
            new TextFieldDefinition { Label = "Text" },
            new IntegerFieldDefinition { Label = "Int" }
        ], out _);

        vm.FillRandomCommand.Execute(null);

        Assert.That(vm.FieldEditors, Has.All.Matches<FieldEditorViewModelBase>(e => !e.GetCurrentValue().IsEmpty));
    }

    [Test]
    public void FillRandom_LeavesMediaEditorsEmpty()
    {
        var vm = BuildVm([new ImageFieldDefinition { Label = "Image" }], out _);

        vm.FillRandomCommand.Execute(null);

        Assert.That(vm.FieldEditors[0].GetCurrentValue().IsEmpty, Is.True);
    }

    [Test]
    public void FillRandom_WhenDisplayNameField_FillsItsEditor()
    {
        var vm = BuildVm([new DisplayNameFieldDefinition()], out _);

        vm.FillRandomCommand.Execute(null);

        var dn = vm.FieldEditors.OfType<DisplayNameFieldEditorViewModel>().Single();
        Assert.That(dn.Text, Is.Not.Empty);
    }

    [Test]
    public void FillRandom_WhenNoDisplayNameField_FillsDisplayNameProperty()
    {
        var vm = BuildVm([new TextFieldDefinition { Label = "Text" }], out _);

        vm.FillRandomCommand.Execute(null);

        Assert.That(vm.DisplayName, Is.Not.Empty);
    }

    [Test]
    public void IsDebugBuild_MatchesTheBuildConfiguration()
    {
        var vm = BuildVm([new TextFieldDefinition { Label = "Text" }], out _);

#if DEBUG
        Assert.That(vm.IsDebugBuild, Is.True);
#else
        Assert.That(vm.IsDebugBuild, Is.False);
#endif
    }
}
