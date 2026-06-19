using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Domain.Import;
using Collectary.Core.Ports;
using Collectary.Core.UseCases.Import;
using FakeItEasy;

namespace Collectary.Core.Tests.UseCases.Import;

[TestFixture]
public class SpreadsheetImportServiceTest
{
    private IItemUseCase _items = null!;
    private IPresetUseCase _presets = null!;
    private SpreadsheetImportService _sut = null!;
    private List<Item> _created = null!;

    [SetUp]
    public void SetUp()
    {
        _items = A.Fake<IItemUseCase>();
        _presets = A.Fake<IPresetUseCase>();
        _created = new List<Item>();
        A.CallTo(() => _items.CreateItemAsync(A<Item>._))
            .Invokes((Item i) => _created.Add(i))
            .Returns(Task.CompletedTask);
        A.CallTo(() => _items.GetItemsForPresetAsync(A<Guid>._)).Returns(new List<Item>());
        _sut = new SpreadsheetImportService(_items, _presets);
    }

    private WorkbookCell Text(string s) => new(s, WorkbookCellKind.Text);
    private WorkbookCell Number(string s) => new(s, WorkbookCellKind.Number);
    private WorkbookCell Blank() => new(null, WorkbookCellKind.Blank);

    private ShapedGrid Grid(IReadOnlyList<string> headers, params IReadOnlyList<WorkbookCell>[] rows) => new(headers, rows);

    [Test]
    public async Task ImportExistingAsync_MapsTitleAndFieldValues()
    {
        var presetId = Guid.NewGuid();
        var notes = new TextFieldDefinition { Label = "Notes" };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new FieldDefinition[] { new DisplayNameFieldDefinition(), notes } });

        var grid = Grid(new[] { "Name", "Notes" },
            new[] { Text("Dune"), Text("good") },
            new[] { Text("Hobbit"), Text("great") });
        var mappings = new[] { new ColumnMapping(0, Guid.Empty, true), new ColumnMapping(1, notes.Id, false) };

        var summary = await _sut.ImportExistingAsync(presetId, grid, mappings, CultureInfo.InvariantCulture);

        Assert.That(summary.Imported, Is.EqualTo(2));
        Assert.That(_created, Has.Count.EqualTo(2));
        Assert.That(_created[0].PresetId, Is.EqualTo(presetId));
        Assert.That(_created[0].DisplayName, Is.EqualTo("Dune"));
        Assert.That(((TextFieldValue)_created[0].Values.Single()).Value, Is.EqualTo("good"));
        Assert.That(_created[0].Values.Single().FieldDefinitionId, Is.EqualTo(notes.Id));
    }

    [Test]
    public async Task ImportExistingAsync_RecordsSkipWhenCreateThrows()
    {
        var presetId = Guid.NewGuid();
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new FieldDefinition[] { new DisplayNameFieldDefinition() } });
        A.CallTo(() => _items.CreateItemAsync(A<Item>._))
            .Throws(new InvalidOperationException("Required fields missing: Notes"));

        var grid = Grid(new[] { "Name" }, new[] { Text("Dune") });
        var mappings = new[] { new ColumnMapping(0, Guid.Empty, true) };

        var summary = await _sut.ImportExistingAsync(presetId, grid, mappings, CultureInfo.InvariantCulture);

        Assert.That(summary.Imported, Is.EqualTo(0));
        Assert.That(summary.Skipped, Has.Count.EqualTo(1));
        Assert.That(summary.Skipped[0].RowNumber, Is.EqualTo(1));
        Assert.That(summary.Skipped[0].Kind, Is.EqualTo(ImportIssueKind.Error));
        Assert.That(summary.Skipped[0].Detail, Does.Contain("Required"));
    }

    [Test]
    public async Task ImportExistingAsync_WarnsOnUnparseableCellButStillImports()
    {
        var presetId = Guid.NewGuid();
        var pages = new IntegerFieldDefinition { Label = "Pages" };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new FieldDefinition[] { new DisplayNameFieldDefinition(), pages } });

        var grid = Grid(new[] { "Name", "Pages" }, new[] { Text("Dune"), Text("abc") });
        var mappings = new[] { new ColumnMapping(0, Guid.Empty, true), new ColumnMapping(1, pages.Id, false) };

        var summary = await _sut.ImportExistingAsync(presetId, grid, mappings, CultureInfo.InvariantCulture);

        Assert.That(summary.Imported, Is.EqualTo(1));
        Assert.That(summary.Warnings, Has.Count.EqualTo(1));
        Assert.That(summary.Warnings[0].Kind, Is.EqualTo(ImportIssueKind.UnparsedCells));
        Assert.That(summary.Warnings[0].Detail, Does.Contain("Pages"));
        Assert.That(_created[0].Values, Is.Empty);
        Assert.That(_created[0].DisplayName, Is.EqualTo("Dune"));
    }

    [Test]
    public async Task ImportExistingAsync_SkipsBlankCellsWithoutWarning()
    {
        var presetId = Guid.NewGuid();
        var notes = new TextFieldDefinition { Label = "Notes" };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new FieldDefinition[] { new DisplayNameFieldDefinition(), notes } });

        var grid = Grid(new[] { "Name", "Notes" }, new[] { Text("Dune"), Blank() });
        var mappings = new[] { new ColumnMapping(0, Guid.Empty, true), new ColumnMapping(1, notes.Id, false) };

        var summary = await _sut.ImportExistingAsync(presetId, grid, mappings, CultureInfo.InvariantCulture);

        Assert.That(summary.Imported, Is.EqualTo(1));
        Assert.That(summary.Warnings, Is.Empty);
        Assert.That(_created[0].Values, Is.Empty);
    }

    [Test]
    public async Task ImportNewAsync_BuildsPresetWithTitleAndInferredFieldsThenImports()
    {
        Preset? captured = null;
        A.CallTo(() => _presets.CreatePresetAsync(A<Preset>._))
            .Invokes((Preset p) => captured = p)
            .Returns(Task.CompletedTask);

        var grid = Grid(new[] { "Title", "Pages", "Year" }, new[] { Text("Dune"), Text("412"), Text("1965") });
        var columns = new[]
        {
            new NewFieldColumn(0, new DisplayNameFieldDefinition(), true),
            new NewFieldColumn(1, new IntegerFieldDefinition { Label = "Pages" }, false),
            new NewFieldColumn(2, new IntegerFieldDefinition { Label = "Year" }, false)
        };

        var result = await _sut.ImportNewAsync("Books", grid, columns, CultureInfo.InvariantCulture);
        var preset = result.Preset;
        var summary = result.Summary;

        Assert.That(captured, Is.SameAs(preset));
        Assert.That(preset.Name, Is.EqualTo("Books"));
        Assert.That(preset.Fields, Has.Count.EqualTo(3));

        var title = preset.Fields.Single(f => f.IsTitleField);
        Assert.That(title.PresetId, Is.EqualTo(preset.Id));
        Assert.That(title.DisplayOrder, Is.EqualTo(0));

        var pages = preset.Fields.Single(f => f.Label == "Pages");
        var year = preset.Fields.Single(f => f.Label == "Year");
        Assert.That(pages.PresetId, Is.EqualTo(preset.Id));
        Assert.That(pages.DisplayOrder, Is.EqualTo(1));
        Assert.That(year.DisplayOrder, Is.EqualTo(2));

        Assert.That(summary.Imported, Is.EqualTo(1));
        Assert.That(_created[0].DisplayName, Is.EqualTo("Dune"));
        Assert.That(_created[0].Values.OfType<IntegerFieldValue>().Select(v => v.Value),
            Is.EquivalentTo(new int?[] { 412, 1965 }));
    }

    [Test]
    public async Task ImportExistingAsync_AutoNumberColumn_ImportsTheNumberVerbatim()
    {
        var presetId = Guid.NewGuid();
        var number = new AutoNumberFieldDefinition { Label = "No" };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new FieldDefinition[] { new DisplayNameFieldDefinition(), number } });

        var grid = Grid(new[] { "Name", "No" }, new[] { Text("Dune"), Text("42") });
        var mappings = new[] { new ColumnMapping(0, Guid.Empty, true), new ColumnMapping(1, number.Id, false) };

        var summary = await _sut.ImportExistingAsync(presetId, grid, mappings, CultureInfo.InvariantCulture);

        Assert.That(summary.Imported, Is.EqualTo(1));
        Assert.That(((AutoNumberFieldValue)_created[0].Values.Single()).Value, Is.EqualTo(42));
    }

    [Test]
    public async Task ImportNewAsync_AutoNumberColumn_IsEditableAndWarnsOnDuplicate()
    {
        Preset? captured = null;
        A.CallTo(() => _presets.CreatePresetAsync(A<Preset>._))
            .Invokes((Preset p) => captured = p)
            .Returns(Task.CompletedTask);

        var grid = Grid(new[] { "Title", "No" }, new[] { Text("Dune"), Text("7") });
        var columns = new[]
        {
            new NewFieldColumn(0, new DisplayNameFieldDefinition(), true),
            new NewFieldColumn(1, new AutoNumberFieldDefinition { Label = "No" }, false)
        };

        await _sut.ImportNewAsync("Books", grid, columns, CultureInfo.InvariantCulture);

        var number = (AutoNumberFieldDefinition)captured!.Fields.Single(f => f.Label == "No");
        Assert.Multiple(() =>
        {
            Assert.That(number.Editable, Is.True, "imported numbers must be editable so the duplicate warning can surface");
            Assert.That(number.OnDuplicate, Is.EqualTo(DuplicateHandling.Warn), "duplicates must warn, never block the save");
        });
        Assert.That(((AutoNumberFieldValue)_created[0].Values.Single()).Value, Is.EqualTo(7));
    }

    [Test]
    public async Task ImportExistingAsync_DuplicateAutoNumbersWithinTheSheet_AreImportedButWarned()
    {
        var presetId = Guid.NewGuid();
        var number = new AutoNumberFieldDefinition { Label = "No", OnDuplicate = DuplicateHandling.Warn };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new FieldDefinition[] { new DisplayNameFieldDefinition(), number } });

        var grid = Grid(new[] { "Name", "No" },
            new[] { Text("Dune"), Text("5") },
            new[] { Text("Hobbit"), Text("5") });
        var mappings = new[] { new ColumnMapping(0, Guid.Empty, true), new ColumnMapping(1, number.Id, false) };

        var summary = await _sut.ImportExistingAsync(presetId, grid, mappings, CultureInfo.InvariantCulture);

        Assert.That(summary.Imported, Is.EqualTo(2), "a duplicate must never block the import");
        Assert.That(summary.Warnings, Is.Empty, "a duplicate is not a left-blank cell and must not inflate that count");
        var dup = summary.Duplicates.Single();
        Assert.Multiple(() =>
        {
            Assert.That(dup.RowNumber, Is.EqualTo(2));
            Assert.That(dup.ItemName, Is.EqualTo("Hobbit"), "the row is identified by the item's name, not its number");
            Assert.That(dup.FieldLabel, Is.EqualTo("No"));
            Assert.That(dup.Value, Is.EqualTo("5"));
        });
        Assert.That(((AutoNumberFieldValue)_created[1].Values.Single()).Value, Is.EqualTo(5),
            "the duplicate value must be imported unchanged, never dropped");
    }

    [Test]
    public async Task ImportExistingAsync_AutoNumberCollidingWithAnExistingItem_IsWarned()
    {
        var presetId = Guid.NewGuid();
        var number = new AutoNumberFieldDefinition { Label = "No", OnDuplicate = DuplicateHandling.Error };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new FieldDefinition[] { new DisplayNameFieldDefinition(), number } });
        A.CallTo(() => _items.GetItemsForPresetAsync(presetId)).Returns(new List<Item>
        {
            new() { Values = { new AutoNumberFieldValue { FieldDefinitionId = number.Id, Value = 5 } } }
        });

        var grid = Grid(new[] { "Name", "No" }, new[] { Text("Dune"), Text("5") });
        var mappings = new[] { new ColumnMapping(0, Guid.Empty, true), new ColumnMapping(1, number.Id, false) };

        var summary = await _sut.ImportExistingAsync(presetId, grid, mappings, CultureInfo.InvariantCulture);

        Assert.That(summary.Imported, Is.EqualTo(1));
        Assert.That(summary.Duplicates, Has.Count.EqualTo(1));
        Assert.That(((AutoNumberFieldValue)_created[0].Values.Single()).Value, Is.EqualTo(5));
    }

    [Test]
    public async Task ImportExistingAsync_DuplicateAutoNumbers_WhenDuplicatesAreAllowed_AreNotWarned()
    {
        var presetId = Guid.NewGuid();
        var number = new AutoNumberFieldDefinition { Label = "No", OnDuplicate = DuplicateHandling.Allow };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new FieldDefinition[] { new DisplayNameFieldDefinition(), number } });

        var grid = Grid(new[] { "Name", "No" },
            new[] { Text("Dune"), Text("5") },
            new[] { Text("Hobbit"), Text("5") });
        var mappings = new[] { new ColumnMapping(0, Guid.Empty, true), new ColumnMapping(1, number.Id, false) };

        var summary = await _sut.ImportExistingAsync(presetId, grid, mappings, CultureInfo.InvariantCulture);

        Assert.That(summary.Imported, Is.EqualTo(2));
        Assert.That(summary.Duplicates, Is.Empty);
    }

    [Test]
    public async Task ImportExistingAsync_DuplicateColumnMappings_WriteSingleValuePerField()
    {
        var presetId = Guid.NewGuid();
        var notes = new TextFieldDefinition { Label = "Notes" };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new FieldDefinition[] { new DisplayNameFieldDefinition(), notes } });

        var grid = Grid(new[] { "Name", "Notes", "Notes" },
            new[] { Text("Dune"), Text("first"), Text("second") });
        var mappings = new[]
        {
            new ColumnMapping(0, Guid.Empty, true),
            new ColumnMapping(1, notes.Id, false),
            new ColumnMapping(2, notes.Id, false)
        };

        var summary = await _sut.ImportExistingAsync(presetId, grid, mappings, CultureInfo.InvariantCulture);

        Assert.That(summary.Imported, Is.EqualTo(1));
        Assert.That(_created[0].Values, Has.Count.EqualTo(1));
        Assert.That(((TextFieldValue)_created[0].Values.Single()).Value, Is.EqualTo("first"));
    }

    [Test]
    public async Task ImportExistingAsync_DuplicateEffectiveFieldIds_DoesNotThrow()
    {
        var presetId = Guid.NewGuid();
        var notes = new TextFieldDefinition { Label = "Notes" };
        var duplicate = new TextFieldDefinition { Id = notes.Id, Label = "Notes (shared)" };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new FieldDefinition[] { new DisplayNameFieldDefinition(), notes, duplicate } });

        var grid = Grid(new[] { "Name", "Notes" }, new[] { Text("Dune"), Text("good") });
        var mappings = new[] { new ColumnMapping(0, Guid.Empty, true), new ColumnMapping(1, notes.Id, false) };

        var summary = await _sut.ImportExistingAsync(presetId, grid, mappings, CultureInfo.InvariantCulture);

        Assert.That(summary.Imported, Is.EqualTo(1));
        Assert.That(((TextFieldValue)_created[0].Values.Single()).Value, Is.EqualTo("good"));
    }

    [Test]
    public async Task ImportExistingAsync_TypedNumberCell_ParsesInvariantNotSourceCulture()
    {
        var presetId = Guid.NewGuid();
        var price = new DecimalFieldDefinition { Label = "Price" };
        A.CallTo(() => _presets.GetEffectiveFieldsAsync(presetId))
            .Returns(new EffectiveFields { Fields = new FieldDefinition[] { new DisplayNameFieldDefinition(), price } });

        var grid = Grid(new[] { "Name", "Price" },
            new[] { Text("Dune"), new WorkbookCell("1234.56", WorkbookCellKind.Number) });
        var mappings = new[] { new ColumnMapping(0, Guid.Empty, true), new ColumnMapping(1, price.Id, false) };

        var summary = await _sut.ImportExistingAsync(presetId, grid, mappings, new CultureInfo("de-DE"));

        Assert.That(summary.Imported, Is.EqualTo(1));
        Assert.That(((DecimalFieldValue)_created[0].Values.Single()).Value, Is.EqualTo(1234.56m));
    }
}
