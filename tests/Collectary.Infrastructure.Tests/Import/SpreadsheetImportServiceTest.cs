using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Domain.Import;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Core.UseCases.Import;
using Collectary.Infrastructure.Persistence;

namespace Collectary.Infrastructure.Tests.Import;

file sealed class AlwaysAllowAuthorization : ICollectionAuthorization
{
    public Task<bool> CanReadAsync(Guid presetId) => Task.FromResult(true);
    public Task<bool> CanWriteAsync(Guid presetId) => Task.FromResult(true);
    public Task<bool> IsOwnerAsync(Guid presetId) => Task.FromResult(true);
}

[TestFixture]
public class SpreadsheetImportServiceTest : DbIntegrationTestBase
{
    private ItemRepository _itemRepo = null!;
    private PresetUseCase _presetUseCase = null!;
    private SpreadsheetImportService _sut = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        var auth = new AlwaysAllowAuthorization();
        var presetRepo = new PresetRepository(DbFactory, new FieldDefinitionMerger());
        _itemRepo = new ItemRepository(DbFactory);
        _presetUseCase = new PresetUseCase(presetRepo, _itemRepo, auth);
        var itemUseCase = new ItemUseCase(_itemRepo, _presetUseCase, auth);
        _sut = new SpreadsheetImportService(itemUseCase, _presetUseCase);
    }

    private WorkbookCell Text(string s) => new(s, WorkbookCellKind.Text);

    [Test]
    public async Task ImportNewAsync_PersistsPresetAndItemsWithGermanCulture()
    {
        var grid = new ShapedGrid(new[] { "Name", "Price" }, new IReadOnlyList<WorkbookCell>[]
        {
            new[] { Text("Dune"), Text("1.234,56") }
        });
        var columns = new[]
        {
            new NewFieldColumn(0, new DisplayNameFieldDefinition(), true),
            new NewFieldColumn(1, new DecimalFieldDefinition { Label = "Price" }, false)
        };

        var (preset, summary) = await _sut.ImportNewAsync("Books", grid, columns, new CultureInfo("de-DE"));

        Assert.That(summary.Imported, Is.EqualTo(1));
        var items = await _itemRepo.GetByPresetAsync(preset.Id);
        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].DisplayName, Is.EqualTo("Dune"));
        var value = (DecimalFieldValue)items[0].Values.Single();
        Assert.That(value.Value, Is.EqualTo(1234.56m));
    }

    [Test]
    public async Task ImportExistingAsync_PersistsItemsIntoExistingPreset()
    {
        var preset = new Preset { Name = "Books" };
        preset.Fields.Add(new DisplayNameFieldDefinition { PresetId = preset.Id });
        var notes = new TextFieldDefinition { Label = "Notes", PresetId = preset.Id };
        preset.Fields.Add(notes);
        await _presetUseCase.CreatePresetAsync(preset);

        var grid = new ShapedGrid(new[] { "Name", "Notes" }, new IReadOnlyList<WorkbookCell>[]
        {
            new[] { Text("Dune"), Text("a classic") },
            new[] { Text("Hobbit"), Text("cosy") }
        });
        var mappings = new[]
        {
            new ColumnMapping(0, Guid.Empty, true),
            new ColumnMapping(1, notes.Id, false)
        };

        var summary = await _sut.ImportExistingAsync(preset.Id, grid, mappings, CultureInfo.InvariantCulture);

        Assert.That(summary.Imported, Is.EqualTo(2));
        var items = await _itemRepo.GetByPresetAsync(preset.Id);
        Assert.That(items.Select(i => i.DisplayName), Is.EquivalentTo(new[] { "Dune", "Hobbit" }));
        Assert.That(items.SelectMany(i => i.Values).OfType<TextFieldValue>().Select(v => v.Value),
            Is.EquivalentTo(new[] { "a classic", "cosy" }));
    }
}
