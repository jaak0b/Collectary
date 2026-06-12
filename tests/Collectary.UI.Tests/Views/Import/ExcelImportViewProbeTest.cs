using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Import;
using Collectary.Core.Ports;
using Collectary.Core.UseCases.Import;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels.Import;
using Collectary.UI.Views.Import;
using FakeItEasy;

namespace Collectary.UI.Tests.Views.Import;

[TestFixture]
public class ExcelImportViewProbeTest
{
    private WorkbookCell Cell(string s) => new(s, WorkbookCellKind.Text);

    private static bool EffectivelyHidden(Control c) =>
        !c.IsVisible || c.GetVisualAncestors().OfType<Control>().Any(a => !a.IsVisible);

    private List<RadioButton> NameRadios(ExcelImportView view, string group) =>
        view.GetVisualDescendants().OfType<RadioButton>().Where(r => r.GroupName == group).ToList();

    private async Task<ExcelImportView> RenderMapStepAsync(double width = 800)
    {
        var data = new WorkbookData(new[]
        {
            new WorkbookSheet("Sheet1", new[]
            {
                (IReadOnlyList<WorkbookCell>)new[] { Cell("Title"), Cell("Pages") },
                new[] { Cell("Dune"), Cell("412") }
            })
        });

        var vm = new ExcelImportViewModel(
            data,
            new GridShaper(),
            new CultureDetector(),
            new FieldTypeInference(),
            A.Fake<ISpreadsheetImportService>(),
            A.Fake<IPresetUseCase>(),
            A.Fake<IDialogService>(),
            Array.Empty<Preset>(),
            onFinished: null,
            onClose: () => { });
        vm.CreateNewCollection = true;
        vm.NewCollectionName = "Imported";
        while (vm.Step != ImportStep.Map)
            await vm.NextCommand.ExecuteAsync(null);

        var view = new ExcelImportView { DataContext = vm };
        var window = new Window { Content = view, Width = width, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return view;
    }

    [Test]
    public async Task MapStep_RendersAColumnHeaderRow()
    {
        var view = await RenderMapStepAsync();

        var header = view.FindControl<Grid>("MapColumnsHeader");
        Assert.That(header, Is.Not.Null, "the Map step must show a header row labelling the table columns");
        Assert.That(header!.IsVisible, Is.True);
    }

    [Test]
    public async Task MapStep_NameRadios_AreMutuallyExclusive()
    {
        var view = await RenderMapStepAsync();

        var radios = view.GetVisualDescendants().OfType<RadioButton>()
            .Where(r => r.GroupName == "ImportName").ToList();
        Assert.That(radios, Has.Count.GreaterThanOrEqualTo(2), "each column needs a Name radio in a shared group");

        radios[0].IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        radios[1].IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.That(radios.Count(r => r.IsChecked == true), Is.EqualTo(1), "exactly one column can be the item name");
        Assert.That(radios[0].IsChecked, Is.False);
    }

    [Test]
    public async Task NarrowViewport_SetsIsNarrowAndHidesTheTableHeader()
    {
        var view = await RenderMapStepAsync(width: 400);
        var vm = (ExcelImportViewModel)view.DataContext!;
        var header = view.FindControl<Grid>("MapColumnsHeader")!;

        Assert.That(vm.IsNarrow, Is.True, "below the narrow threshold the view must flag itself narrow");
        Assert.That(header.IsVisible, Is.False, "the table header makes no sense once columns become cards");
        Assert.That(NameRadios(view, "ImportName").All(EffectivelyHidden), Is.True,
            "the wide table is hidden on a narrow screen");
        Assert.That(NameRadios(view, "ImportNameNarrow").Any(r => !EffectivelyHidden(r)), Is.True,
            "the cards are shown on a narrow screen");
    }

    [Test]
    public async Task WideViewport_KeepsTheTableHeader()
    {
        var view = await RenderMapStepAsync(width: 1000);
        var vm = (ExcelImportViewModel)view.DataContext!;
        var header = view.FindControl<Grid>("MapColumnsHeader")!;

        Assert.That(vm.IsNarrow, Is.False);
        Assert.That(header.IsVisible, Is.True);
        Assert.That(NameRadios(view, "ImportName").Any(r => !EffectivelyHidden(r)), Is.True,
            "the wide table is shown on a wide screen");
        Assert.That(NameRadios(view, "ImportNameNarrow").All(EffectivelyHidden), Is.True,
            "the cards are hidden on a wide screen");
    }

    [Test]
    public async Task NarrowCards_NameRadios_AreMutuallyExclusive()
    {
        var view = await RenderMapStepAsync(width: 400);

        var radios = view.GetVisualDescendants().OfType<RadioButton>()
            .Where(r => r.GroupName == "ImportNameNarrow").ToList();
        Assert.That(radios, Has.Count.GreaterThanOrEqualTo(2), "each narrow card needs its own Name radio");

        radios[0].IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        radios[1].IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.That(radios.Count(r => r.IsChecked == true), Is.EqualTo(1), "exactly one card can be the item name");
        Assert.That(radios[0].IsChecked, Is.False);
    }
}
