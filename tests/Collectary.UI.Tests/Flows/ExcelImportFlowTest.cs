using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Domain.Import;
using Collectary.Core.UseCases.Import;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels.Import;
using Collectary.UI.Tests.Infrastructure;

namespace Collectary.UI.Tests.Flows;

[TestFixture]
public class ExcelImportFlowTest : FlowTestBase
{
    private WorkbookCell Cell(string s) => new(s, WorkbookCellKind.Text);

    private WorkbookData Workbook(string sheetName, params IReadOnlyList<WorkbookCell>[] rows) =>
        new(new[] { new WorkbookSheet(sheetName, rows) });

    private ExcelImportViewModel MakeVm(WorkbookData data, IReadOnlyList<Preset> presets, Func<Preset, Task>? onFinished = null) =>
        new(
            data,
            new GridShaper(),
            new CultureDetector(),
            new FieldTypeInference(),
            new SpreadsheetImportService(ItemUseCase, PresetUseCase),
            PresetUseCase,
            A.Fake<IDialogService>(),
            presets,
            onFinished,
            onClose: () => { });

    private async Task<Preset> CreateBooksPresetAsync(params FieldDefinition[] extra)
    {
        var preset = new Preset { Name = "Books" };
        preset.Fields.Add(new DisplayNameFieldDefinition { PresetId = preset.Id });
        foreach (var f in extra)
        {
            f.PresetId = preset.Id;
            preset.Fields.Add(f);
        }
        await PresetUseCase.CreatePresetAsync(preset);
        return preset;
    }

    [Test]
    public void Construction_PopulatesSheetsAndColumns()
    {
        var data = Workbook("Sheet1", new[] { Cell("Name"), Cell("Notes") }, new[] { Cell("Dune"), Cell("good") });
        var vm = MakeVm(data, Array.Empty<Preset>());

        Assert.That(vm.SheetNames, Is.EqualTo(new[] { "Sheet1" }));
        Assert.That(vm.SelectedSheetName, Is.EqualTo("Sheet1"));
        Assert.That(vm.ColumnHeaders, Is.EqualTo(new[] { "Name", "Notes" }));
        Assert.That(vm.PreviewRows, Has.Count.EqualTo(1));
    }

    [Test]
    public void HeaderToggle_RecomputesHeadersAndRows()
    {
        var data = Workbook("Sheet1", new[] { Cell("Name"), Cell("Notes") }, new[] { Cell("Dune"), Cell("good") });
        var vm = MakeVm(data, Array.Empty<Preset>());

        Assert.That(vm.PreviewRows, Has.Count.EqualTo(1));

        vm.FirstRowIsHeader = false;

        Assert.That(vm.PreviewRows, Has.Count.EqualTo(2));
        Assert.That(vm.ColumnHeaders[0], Does.Contain("1"));
    }

    [Test]
    public void Transpose_SwapsColumnCount()
    {
        var data = Workbook("Sheet1",
            new[] { Cell("a"), Cell("b"), Cell("c") },
            new[] { Cell("d"), Cell("e"), Cell("f") });
        var vm = MakeVm(data, Array.Empty<Preset>());
        vm.FirstRowIsHeader = false;

        Assert.That(vm.ColumnHeaders, Has.Count.EqualTo(3));

        vm.Transpose = true;

        Assert.That(vm.ColumnHeaders, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ExistingImport_PersistsItems()
    {
        var notes = new TextFieldDefinition { Label = "Notes" };
        var preset = await CreateBooksPresetAsync(notes);
        var data = Workbook("Sheet1",
            new[] { Cell("Name"), Cell("Notes") },
            new[] { Cell("Dune"), Cell("a classic") },
            new[] { Cell("Hobbit"), Cell("cosy") });
        var vm = MakeVm(data, new[] { preset });
        vm.CreateNewCollection = false;
        vm.SelectedPreset = preset;

        await vm.NextCommand.ExecuteAsync(null); // Sheet -> Preview
        await vm.NextCommand.ExecuteAsync(null); // Preview -> Target
        await vm.NextCommand.ExecuteAsync(null); // Target -> Map

        vm.Columns[0].SelectedTarget = vm.Columns[0].TargetOptions.First(o => o.IsTitle);
        vm.Columns[1].SelectedTarget = vm.Columns[1].TargetOptions.First(o => o.Field?.Id == notes.Id);

        await vm.NextCommand.ExecuteAsync(null); // Map -> import

        Assert.That(vm.Step, Is.EqualTo(ImportStep.Result));
        Assert.That(vm.Summary!.Imported, Is.EqualTo(2));
        var items = await ItemRepo.GetByPresetAsync(preset.Id);
        Assert.That(items.Select(i => i.DisplayName), Is.EquivalentTo(new[] { "Dune", "Hobbit" }));
    }

    [Test]
    public async Task UnmappableFieldType_IsDisabledInTargets()
    {
        var preset = await CreateBooksPresetAsync(new ImageFieldDefinition { Label = "Cover" });
        var data = Workbook("Sheet1", new[] { Cell("Name") }, new[] { Cell("Dune") });
        var vm = MakeVm(data, new[] { preset });
        vm.SelectedPreset = preset;

        await vm.NextCommand.ExecuteAsync(null);
        await vm.NextCommand.ExecuteAsync(null);
        await vm.NextCommand.ExecuteAsync(null);

        var cover = vm.Columns.SelectMany(c => c.TargetOptions).First(o => o.Field is ImageFieldDefinition);
        Assert.That(cover.IsMappable, Is.False);
    }

    [Test]
    public async Task NewCollectionImport_CreatesPresetAndItems()
    {
        var data = Workbook("Sheet1",
            new[] { Cell("Title"), Cell("Pages") },
            new[] { Cell("Dune"), Cell("412") });
        var vm = MakeVm(data, Array.Empty<Preset>());
        vm.CreateNewCollection = true;
        vm.NewCollectionName = "Imported";

        await vm.NextCommand.ExecuteAsync(null);
        await vm.NextCommand.ExecuteAsync(null);
        await vm.NextCommand.ExecuteAsync(null);
        await vm.NextCommand.ExecuteAsync(null);

        Assert.That(vm.Step, Is.EqualTo(ImportStep.Result));
        Assert.That(vm.Summary!.Imported, Is.EqualTo(1));
        var presets = await PresetUseCase.GetAllPresetsAsync();
        Assert.That(presets.Select(p => p.Name), Does.Contain("Imported"));
    }
}
