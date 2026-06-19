using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Domain.Import;
using Collectary.Core.UseCases.Import;
using Collectary.Presentation.Localization;
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

    private async Task AdvanceToMapAsync(ExcelImportViewModel vm)
    {
        while (vm.Step != ImportStep.Map)
            await vm.NextCommand.ExecuteAsync(null);
    }

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
        Assert.That(vm.Columns, Has.Count.EqualTo(2), "recompute rebuilds the column rows from scratch, not on top of the old ones");
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

        await AdvanceToMapAsync(vm);

        vm.Columns[0].IsTitle = true;
        vm.Columns[1].SelectedTarget = vm.Columns[1].TargetOptions.First(o => o.Field?.Id == notes.Id);

        await vm.NextCommand.ExecuteAsync(null); // Map -> import

        Assert.That(vm.Step, Is.EqualTo(ImportStep.Result));
        Assert.That(vm.Summary!.Imported, Is.EqualTo(2));
        var items = await ItemRepo.GetByPresetAsync(preset.Id);
        Assert.That(items.Select(i => i.DisplayName), Is.EquivalentTo(new[] { "Dune", "Hobbit" }));
    }

    [Test]
    public async Task ExistingImport_DuplicateHeaderColumns_WriteSingleValue()
    {
        var notes = new TextFieldDefinition { Label = "Notes" };
        var preset = await CreateBooksPresetAsync(notes);
        var data = Workbook("Sheet1",
            new[] { Cell("Name"), Cell("Notes"), Cell("Notes") },
            new[] { Cell("Dune"), Cell("first"), Cell("second") });
        var vm = MakeVm(data, new[] { preset });
        vm.SelectedPreset = preset;

        await AdvanceToMapAsync(vm);

        vm.Columns[0].IsTitle = true;
        vm.Columns[1].SelectedTarget = vm.Columns[1].TargetOptions.First(o => o.Field?.Id == notes.Id);
        vm.Columns[2].SelectedTarget = vm.Columns[2].TargetOptions.First(o => o.Field?.Id == notes.Id);

        await vm.NextCommand.ExecuteAsync(null);

        Assert.That(vm.Summary!.Imported, Is.EqualTo(1));
        var items = await ItemRepo.GetByPresetAsync(preset.Id);
        Assert.That(items.Single().Values.OfType<TextFieldValue>().Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task DuplicateHeaders_AutoMapOnlyFirstColumnToField()
    {
        var notes = new TextFieldDefinition { Label = "Notes" };
        var preset = await CreateBooksPresetAsync(notes);
        var data = Workbook("Sheet1",
            new[] { Cell("Name"), Cell("Notes"), Cell("Notes") },
            new[] { Cell("Dune"), Cell("a"), Cell("b") });
        var vm = MakeVm(data, new[] { preset });
        vm.SelectedPreset = preset;

        await AdvanceToMapAsync(vm);

        Assert.That(vm.Columns[1].SelectedTarget!.Field?.Id, Is.EqualTo(notes.Id));
        Assert.That(vm.Columns[2].SelectedTarget!.IsSkip, Is.True);
    }

    [Test]
    public async Task HeaderOnlySheet_NextFromPreviewWarnsAndStays()
    {
        var data = Workbook("Sheet1", new[] { Cell("Name") });
        var vm = MakeVm(data, Array.Empty<Preset>());
        Assert.That(vm.Step, Is.EqualTo(ImportStep.Preview));

        await vm.NextCommand.ExecuteAsync(null);

        Assert.That(vm.Step, Is.EqualTo(ImportStep.Preview));
    }

    [Test]
    public async Task UnmappableFieldType_IsDisabledInTargets()
    {
        var preset = await CreateBooksPresetAsync(new ImageFieldDefinition { Label = "Cover" });
        var data = Workbook("Sheet1", new[] { Cell("Name") }, new[] { Cell("Dune") });
        var vm = MakeVm(data, new[] { preset });
        vm.SelectedPreset = preset;

        await AdvanceToMapAsync(vm);

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

        await AdvanceToMapAsync(vm);
        await vm.NextCommand.ExecuteAsync(null); // Map -> import

        Assert.That(vm.Step, Is.EqualTo(ImportStep.Result));
        Assert.That(vm.Summary!.Imported, Is.EqualTo(1));
        var presets = await PresetUseCase.GetAllPresetsAsync();
        Assert.That(presets.Select(p => p.Name), Does.Contain("Imported"));
    }

    [Test]
    public void SummaryText_LocalizesIssueReasons()
    {
        var data = Workbook("Sheet1", new[] { Cell("Name") }, new[] { Cell("Dune") });
        var vm = MakeVm(data, Array.Empty<Preset>());
        vm.Summary = new ImportSummary(
            0,
            new[] { new ImportIssue(2, ImportIssueKind.NoValues, "Pages: 'abc'") },
            new[] { new ImportIssue(3, ImportIssueKind.UnparsedCells, "Pages: 'xyz'") },
            Array.Empty<DuplicateValueRow>());

        try
        {
            LocalizationService.Instance.Apply("de");
            Assert.That(vm.SummarySkippedText, Does.Contain("Werte"));
            Assert.That(vm.SummaryWarningsText, Does.Contain("Zellen"));
        }
        finally
        {
            LocalizationService.Instance.Apply("en");
        }
    }

    [Test]
    public void DuplicateRows_FallBackToRowNumber_WhenTheItemHasNoName()
    {
        var data = Workbook("Sheet1", new[] { Cell("Name") }, new[] { Cell("Dune") });
        var vm = MakeVm(data, Array.Empty<Preset>());
        vm.Summary = new ImportSummary(
            1,
            Array.Empty<ImportIssue>(),
            Array.Empty<ImportIssue>(),
            new[] { new DuplicateValueRow(7, null, "No", "5") });

        var dup = vm.DuplicateRows.Single();
        Assert.Multiple(() =>
        {
            Assert.That(dup.Item, Is.EqualTo("#7"), "with no name, fall back to the row number");
            Assert.That(dup.Field, Is.EqualTo("No"));
            Assert.That(dup.Value, Is.EqualTo("5"));
        });
    }

    [Test]
    public async Task SelectingNameColumn_ClearsNameOnOtherColumns()
    {
        var data = Workbook("Sheet1",
            new[] { Cell("Title"), Cell("Pages") },
            new[] { Cell("Dune"), Cell("412") });
        var vm = MakeVm(data, Array.Empty<Preset>());
        vm.CreateNewCollection = true;
        vm.NewCollectionName = "Imported";

        await AdvanceToMapAsync(vm);
        vm.Columns[0].IsTitle = true;

        vm.Columns[1].IsTitle = true;

        Assert.That(vm.Columns.Count(c => c.IsTitle), Is.EqualTo(1));
        Assert.That(vm.Columns[0].IsTitle, Is.False);
        Assert.That(vm.Columns[1].IsTitle, Is.True);
    }

    [Test]
    public async Task ExistingTargetOptions_OmitTheTitleOption()
    {
        var notes = new TextFieldDefinition { Label = "Notes" };
        var preset = await CreateBooksPresetAsync(notes);
        var data = Workbook("Sheet1",
            new[] { Cell("Name"), Cell("Notes") },
            new[] { Cell("Dune"), Cell("a classic") });
        var vm = MakeVm(data, new[] { preset });
        vm.SelectedPreset = preset;

        await AdvanceToMapAsync(vm);

        Assert.That(vm.Columns.SelectMany(c => c.TargetOptions).Any(o => o.IsTitle), Is.False,
            "the item-name choice now lives on the per-row radio, not inside the target dropdown");
    }

    [Test]
    public async Task ExistingImport_TitleComesFromNameRadio()
    {
        var notes = new TextFieldDefinition { Label = "Notes" };
        var preset = await CreateBooksPresetAsync(notes);
        var data = Workbook("Sheet1",
            new[] { Cell("Name"), Cell("Notes") },
            new[] { Cell("Dune"), Cell("a classic") },
            new[] { Cell("Hobbit"), Cell("cosy") });
        var vm = MakeVm(data, new[] { preset });
        vm.SelectedPreset = preset;

        await AdvanceToMapAsync(vm);
        vm.Columns[0].IsTitle = true;
        vm.Columns[1].SelectedTarget = vm.Columns[1].TargetOptions.First(o => o.Field?.Id == notes.Id);

        await vm.NextCommand.ExecuteAsync(null);

        var items = await ItemRepo.GetByPresetAsync(preset.Id);
        Assert.That(items.Select(i => i.DisplayName), Is.EquivalentTo(new[] { "Dune", "Hobbit" }));
    }

    [Test]
    public async Task ExistingMode_DefaultNameColumn_MatchesThePresetTitleFieldHeader()
    {
        var preset = new Preset { Name = "Books" };
        preset.Fields.Add(new DisplayNameFieldDefinition { PresetId = preset.Id, Label = "Title" });
        var year = new TextFieldDefinition { Label = "Year", PresetId = preset.Id };
        preset.Fields.Add(year);
        await PresetUseCase.CreatePresetAsync(preset);

        var data = Workbook("Sheet1",
            new[] { Cell("Year"), Cell("Title") },
            new[] { Cell("1984"), Cell("Dune") });
        var vm = MakeVm(data, new[] { preset });
        vm.SelectedPreset = preset;

        await AdvanceToMapAsync(vm);

        Assert.That(vm.Columns[1].IsTitle, Is.True, "the column whose header matches the title field is the default name");
        Assert.That(vm.Columns[0].IsTitle, Is.False);
    }

    [Test]
    public async Task ExistingMode_DefaultNameColumn_FallsBackToFirstColumn_WhenNoHeaderMatches()
    {
        var preset = new Preset { Name = "Books" };
        preset.Fields.Add(new DisplayNameFieldDefinition { PresetId = preset.Id, Label = "Title" });
        var notes = new TextFieldDefinition { Label = "Notes", PresetId = preset.Id };
        preset.Fields.Add(notes);
        await PresetUseCase.CreatePresetAsync(preset);

        var data = Workbook("Sheet1",
            new[] { Cell("Author"), Cell("Notes") },
            new[] { Cell("Herbert"), Cell("classic") });
        var vm = MakeVm(data, new[] { preset });
        vm.SelectedPreset = preset;

        await AdvanceToMapAsync(vm);

        Assert.That(vm.Columns[0].IsTitle, Is.True, "with no header matching the title field, the first column is the default name");
        Assert.That(vm.Columns.Count(c => c.IsTitle), Is.EqualTo(1));
    }

    [Test]
    public async Task ChangingAnUnrelatedColumnProperty_DoesNotMoveTheNameSelection()
    {
        var data = Workbook("Sheet1",
            new[] { Cell("Title"), Cell("Pages") },
            new[] { Cell("Dune"), Cell("412") });
        var vm = MakeVm(data, Array.Empty<Preset>());
        vm.CreateNewCollection = true;
        vm.NewCollectionName = "Imported";

        await AdvanceToMapAsync(vm);
        vm.Columns[0].IsTitle = true;

        vm.Columns[1].IsSelected = false;
        vm.Columns[1].Label = "renamed";

        Assert.That(vm.Columns[0].IsTitle, Is.True, "toggling a different column's properties must not steal the name selection");
        Assert.That(vm.Columns.Count(c => c.IsTitle), Is.EqualTo(1));
    }

    [Test]
    public async Task ExistingMode_DefaultNameColumn_IgnoresADeselectedMatchingColumn()
    {
        var preset = new Preset { Name = "Books" };
        preset.Fields.Add(new DisplayNameFieldDefinition { PresetId = preset.Id, Label = "Title" });
        var year = new TextFieldDefinition { Label = "Year", PresetId = preset.Id };
        preset.Fields.Add(year);
        await PresetUseCase.CreatePresetAsync(preset);

        var data = Workbook("Sheet1",
            new[] { Cell("Title"), Cell("Year") },
            new[] { Cell("Dune"), Cell("1984") });
        var vm = MakeVm(data, new[] { preset });
        vm.SelectedPreset = preset;
        vm.Columns[0].IsSelected = false;

        await AdvanceToMapAsync(vm);

        Assert.That(vm.Columns[0].IsTitle, Is.False, "a deselected column can't become the name even if its header matches");
        Assert.That(vm.Columns[1].IsTitle, Is.True);
    }

    [Test]
    public async Task ExistingMode_DeselectedAndSkippedColumns_AreNotImported()
    {
        var notes = new TextFieldDefinition { Label = "Notes" };
        var tags = new TextFieldDefinition { Label = "Tags" };
        var preset = await CreateBooksPresetAsync(notes, tags);
        var data = Workbook("Sheet1",
            new[] { Cell("Name"), Cell("Notes"), Cell("Extra"), Cell("Tags") },
            new[] { Cell("Dune"), Cell("classic"), Cell("ignore me"), Cell("sci-fi") });
        var vm = MakeVm(data, new[] { preset });
        vm.SelectedPreset = preset;

        await AdvanceToMapAsync(vm);
        vm.Columns[0].IsTitle = true;
        Assert.That(vm.Columns[1].SelectedTarget!.Field?.Id, Is.EqualTo(notes.Id), "the Notes column auto-matches the Notes field");
        vm.Columns[1].IsSelected = false;
        vm.Columns[3].SelectedTarget = vm.Columns[3].TargetOptions.First(o => o.Field?.Id == tags.Id);

        await vm.NextCommand.ExecuteAsync(null);

        var item = (await ItemRepo.GetByPresetAsync(preset.Id)).Single();
        Assert.That(item.DisplayName, Is.EqualTo("Dune"));
        Assert.That(item.Values.OfType<TextFieldValue>().Select(v => v.FieldDefinitionId),
            Is.EqualTo(new[] { tags.Id }), "a deselected column is dropped even though it matched a field; the skipped one too");
    }

    [Test]
    public async Task ExistingMode_AColumnMarkedAsName_DoesNotAlsoFillItsMatchedField()
    {
        var notes = new TextFieldDefinition { Label = "Notes" };
        var preset = await CreateBooksPresetAsync(notes);
        var data = Workbook("Sheet1",
            new[] { Cell("Notes"), Cell("Pages") },
            new[] { Cell("Dune"), Cell("x") });
        var vm = MakeVm(data, new[] { preset });
        vm.SelectedPreset = preset;

        await AdvanceToMapAsync(vm);
        Assert.That(vm.Columns[0].SelectedTarget!.Field?.Id, Is.EqualTo(notes.Id), "the Notes column auto-matches the Notes field");
        vm.Columns[0].IsTitle = true;

        await vm.NextCommand.ExecuteAsync(null);

        var item = (await ItemRepo.GetByPresetAsync(preset.Id)).Single();
        Assert.That(item.DisplayName, Is.EqualTo("Dune"));
        Assert.That(item.Values.OfType<TextFieldValue>(), Is.Empty,
            "a column used as the name must not also populate the field it happened to match");
    }

    [Test]
    public async Task ExistingMode_AnUnmappableTargetColumn_IsLeftOutOfTheImport()
    {
        var notes = new TextFieldDefinition { Label = "Notes" };
        var cover = new ImageFieldDefinition { Label = "Cover" };
        var preset = await CreateBooksPresetAsync(notes, cover);
        var data = Workbook("Sheet1",
            new[] { Cell("Name"), Cell("Notes"), Cell("Cover") },
            new[] { Cell("Dune"), Cell("classic"), Cell("http://img") });
        var vm = MakeVm(data, new[] { preset });
        vm.SelectedPreset = preset;

        await AdvanceToMapAsync(vm);
        vm.Columns[0].IsTitle = true;
        vm.Columns[1].SelectedTarget = vm.Columns[1].TargetOptions.First(o => o.Field?.Id == notes.Id);
        vm.Columns[2].SelectedTarget = vm.Columns[2].TargetOptions.First(o => o.Field?.Id == cover.Id);

        await vm.NextCommand.ExecuteAsync(null);

        Assert.That(vm.Summary!.Imported, Is.EqualTo(1));
        Assert.That(vm.Summary.Warnings, Is.Empty,
            "an unmappable column must be dropped while mapping, not carried through as an unreadable cell");
    }

    [Test]
    public async Task ExistingMode_AutoNumberField_IsMappable()
    {
        var preset = await CreateBooksPresetAsync(new AutoNumberFieldDefinition { Label = "No" });
        var data = Workbook("Sheet1", new[] { Cell("Name") }, new[] { Cell("Dune") });
        var vm = MakeVm(data, new[] { preset });
        vm.SelectedPreset = preset;

        await AdvanceToMapAsync(vm);

        var option = vm.Columns.SelectMany(c => c.TargetOptions).First(o => o.Field is AutoNumberFieldDefinition);
        Assert.That(option.IsMappable, Is.True, "an AutoNumber field must be selectable as an import target");
    }

    [Test]
    public async Task NewCollectionImport_AutoNumberColumn_ImportsValueAsEditableWarnField()
    {
        var data = Workbook("Sheet1",
            new[] { Cell("Title"), Cell("No") },
            new[] { Cell("Dune"), Cell("42") });
        var vm = MakeVm(data, Array.Empty<Preset>());
        vm.CreateNewCollection = true;
        vm.NewCollectionName = "Imported";

        await AdvanceToMapAsync(vm);
        vm.Columns[0].IsTitle = true;
        var numberColumn = vm.Columns[1];
        numberColumn.SelectedTypeChoice = numberColumn.TypeChoices.First(t => t.Type == typeof(AutoNumberFieldDefinition));

        await vm.NextCommand.ExecuteAsync(null);

        Assert.That(vm.Summary!.Imported, Is.EqualTo(1));
        var preset = (await PresetUseCase.GetAllPresetsAsync()).Single(p => p.Name == "Imported");
        var number = (AutoNumberFieldDefinition)preset.Fields.Single(f => f.Label == "No");
        Assert.Multiple(() =>
        {
            Assert.That(number.Editable, Is.True);
            Assert.That(number.OnDuplicate, Is.EqualTo(DuplicateHandling.Warn));
        });
        var items = await ItemRepo.GetByPresetAsync(preset.Id);
        Assert.That(((AutoNumberFieldValue)items.Single().Values.Single()).Value, Is.EqualTo(42));
    }

    [Test]
    public async Task NewCollectionImport_DuplicateAutoNumbers_AreReportedInTheSummary()
    {
        var data = Workbook("Sheet1",
            new[] { Cell("Title"), Cell("No") },
            new[] { Cell("Dune"), Cell("5") },
            new[] { Cell("Hobbit"), Cell("5") });
        var vm = MakeVm(data, Array.Empty<Preset>());
        vm.CreateNewCollection = true;
        vm.NewCollectionName = "Imported";

        await AdvanceToMapAsync(vm);
        vm.Columns[0].IsTitle = true;
        var numberColumn = vm.Columns[1];
        numberColumn.SelectedTypeChoice = numberColumn.TypeChoices.First(t => t.Type == typeof(AutoNumberFieldDefinition));

        await vm.NextCommand.ExecuteAsync(null);

        Assert.That(vm.Summary!.Imported, Is.EqualTo(2));
        Assert.That(vm.HasWarnings, Is.False, "a duplicate must not be reported as a cell left blank");
        Assert.That(vm.HasDuplicates, Is.True);
        var dup = vm.DuplicateRows.Single();
        Assert.Multiple(() =>
        {
            Assert.That(dup.Item, Is.EqualTo("Hobbit"), "the duplicate row is named by the item, not a row number");
            Assert.That(dup.Field, Is.EqualTo("No"));
            Assert.That(dup.Value, Is.EqualTo("5"));
        });
        var items = await ItemRepo.GetByPresetAsync((await PresetUseCase.GetAllPresetsAsync()).Single(p => p.Name == "Imported").Id);
        Assert.That(items.SelectMany(i => i.Values).OfType<AutoNumberFieldValue>().Select(v => v.Value),
            Is.EquivalentTo(new int?[] { 5, 5 }), "both duplicate rows must be imported unchanged");
    }

    [Test]
    public void SingleSheet_SkipsSheetStep()
    {
        var data = Workbook("Sheet1", new[] { Cell("Name") }, new[] { Cell("Dune") });
        var vm = MakeVm(data, Array.Empty<Preset>());

        Assert.That(vm.Step, Is.EqualTo(ImportStep.Preview));
        Assert.That(vm.IsSheetStep, Is.False);
    }

    [Test]
    public void MultipleSheets_StartAtSheetStep()
    {
        var data = new WorkbookData(new[]
        {
            new WorkbookSheet("One", new[] { (IReadOnlyList<WorkbookCell>)new[] { Cell("a") } }),
            new WorkbookSheet("Two", new[] { (IReadOnlyList<WorkbookCell>)new[] { Cell("b") } })
        });
        var vm = MakeVm(data, Array.Empty<Preset>());

        Assert.That(vm.Step, Is.EqualTo(ImportStep.Sheet));
        Assert.That(vm.SheetNames, Is.EqualTo(new[] { "One", "Two" }));
    }
}
