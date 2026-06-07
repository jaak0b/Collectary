using Avalonia.Controls;
using Avalonia.Threading;
using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.ListCells;
using Collectary.UI.Views;

namespace Collectary.UI.Tests.Views;

[TestFixture]
public class PresetDetailViewTest
{
    private static async Task<PresetDetailViewModel> LoadedVmWithOneColumn()
    {
        var itemUseCase = A.Fake<IItemUseCase>();
        var presetUseCase = A.Fake<IPresetUseCase>();
        var listCellBuilder = A.Fake<IListCellBuilder>();
        var dialogService = A.Fake<IDialogService>();

        var preset = new Preset { Name = "Test" };
        var field = new TextFieldDefinition { Label = "Name", ShowInList = true };
        A.CallTo(() => presetUseCase.GetEffectiveFieldsAsync(preset.Id))
            .Returns(new EffectiveFields { Fields = new List<FieldDefinition> { field } });
        A.CallTo(() => itemUseCase.GetItemsForPresetAsync(A<Guid>._)).Returns(new List<Item>());
        A.CallTo(() => listCellBuilder.HasListCellViewModel(typeof(TextFieldDefinition))).Returns(true);
        A.CallTo(() => listCellBuilder.Build(A<IReadOnlyList<FieldValue>>._, A<IReadOnlyList<FieldDefinition>>._))
            .Returns((IReadOnlyList<ListCellViewModelBase>)new List<ListCellViewModelBase>());

        var vm = new PresetDetailViewModel(preset, itemUseCase, presetUseCase, listCellBuilder, dialogService,
            navigateToItemEditor: (_, _, _) => { }, navigateBack: () => { });
        await vm.LoadAsync();
        return vm;
    }

    [Test]
    public async Task ActionColumn_IsFirstAndFrozen_SoItStaysReachableWhenGridOverflows()
    {
        var vm = await LoadedVmWithOneColumn();

        var view = new PresetDetailView { DataContext = vm };
        Dispatcher.UIThread.RunJobs();

        var grid = view.FindControl<DataGrid>("ItemGrid")!;

        Assert.Multiple(() =>
        {
            Assert.That(grid.FrozenColumnCount, Is.EqualTo(1), "the action column must be frozen so it never scrolls off screen");
            Assert.That(grid.Columns, Has.Count.GreaterThan(1));
            Assert.That(grid.Columns[0].Header, Is.EqualTo(""), "the action (⋯) column must be the first, frozen column");
        });
    }
}
