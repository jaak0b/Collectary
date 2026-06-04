using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Infrastructure.Persistence;

namespace Collectary.Infrastructure.Tests.Sync;

[TestFixture]
public class EfSyncStoreTest : DbIntegrationTestBase
{
    private EfSyncStore _sut = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _sut = new EfSyncStore(DbFactory, new FieldDefinitionMerger());
    }

    private static Preset MakePreset(Guid id, string name)
    {
        var preset = new Preset { Id = id, Name = name, Revision = 1 };
        preset.Fields.Add(new TextFieldDefinition { Label = "Title", PresetId = id });
        return preset;
    }

    [Test]
    public async Task ApplyPresetAsync_InsertsAndGetAllReturnsIt()
    {
        var id = Guid.NewGuid();
        await _sut.ApplyPresetAsync(MakePreset(id, "Trains"));

        var all = await _sut.GetAllPresetsAsync();

        Assert.That(all.Single(p => p.Id == id).Fields.Single().Label, Is.EqualTo("Title"));
    }

    [Test]
    public async Task ApplyPresetAsync_UpsertsWithoutDuplicating()
    {
        var id = Guid.NewGuid();
        await _sut.ApplyPresetAsync(MakePreset(id, "Trains"));
        await _sut.ApplyPresetAsync(MakePreset(id, "Planes"));

        var all = await _sut.GetAllPresetsAsync();

        Assert.That(all.Where(p => p.Id == id).Select(p => p.Name), Is.EqualTo(new[] { "Planes" }));
    }

    [Test]
    public async Task ApplyItemAsync_RoundTripsValues()
    {
        var presetId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var preset = new Preset { Id = presetId, Name = "P", Revision = 1 };
        preset.Fields.Add(new TextFieldDefinition { Id = fieldId, Label = "Title", PresetId = presetId });
        await _sut.ApplyPresetAsync(preset);

        var id = Guid.NewGuid();
        var item = new Item { Id = id, DisplayName = "Loco", PresetId = presetId, Revision = 1 };
        item.Values.Add(new TextFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = fieldId, Value = "hi", ItemId = id });

        await _sut.ApplyItemAsync(item);

        var all = await _sut.GetAllItemsAsync();
        Assert.That(((TextFieldValue)all.Single(i => i.Id == id).Values.Single()).Value, Is.EqualTo("hi"));
    }

    [Test]
    public void MarkSyncedAsync_WithUnknownKind_Throws()
    {
        Assert.That(async () => await _sut.MarkSyncedAsync((SyncEntityKind)999, Guid.NewGuid(), 1, false),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public async Task ApplyItemAsync_WhenAddFails_RollsBackAndKeepsExisting()
    {
        var presetId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var preset = new Preset { Id = presetId, Name = "P", Revision = 1 };
        preset.Fields.Add(new TextFieldDefinition { Id = fieldId, Label = "Title", PresetId = presetId });
        await _sut.ApplyPresetAsync(preset);

        var id = Guid.NewGuid();
        var good = new Item { Id = id, DisplayName = "Good", PresetId = presetId, Revision = 1 };
        good.Values.Add(new TextFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = fieldId, Value = "ok", ItemId = id });
        await _sut.ApplyItemAsync(good);

        var bad = new Item { Id = id, DisplayName = "Bad", PresetId = presetId, Revision = 2 };
        bad.Values.Add(new TextFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = Guid.NewGuid(), Value = "x", ItemId = id });

        Assert.That(async () => await _sut.ApplyItemAsync(bad), Throws.Exception);

        var all = await _sut.GetAllItemsAsync();
        var survivor = all.Single(i => i.Id == id);
        Assert.That(survivor.DisplayName, Is.EqualTo("Good"), "a failed apply must not destroy the existing aggregate");
    }

    [Test]
    public async Task ApplySystemFieldAsync_InsertsThenUpdatesInPlace()
    {
        var id = Guid.NewGuid();
        await _sut.ApplySystemFieldAsync(new SystemField
        {
            Id = id, Name = "A", Revision = 1, Definition = new TextFieldDefinition { SystemFieldId = id },
        });
        await _sut.ApplySystemFieldAsync(new SystemField
        {
            Id = id, Name = "B", Revision = 2, Definition = new TextFieldDefinition { SystemFieldId = id },
        });

        var all = await _sut.GetAllSystemFieldsAsync();
        var field = all.Single(sf => sf.Id == id);
        Assert.Multiple(() =>
        {
            Assert.That(all.Count(sf => sf.Id == id), Is.EqualTo(1));
            Assert.That(field.Name, Is.EqualTo("B"));
            Assert.That(field.Revision, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task GetAllPresetsAsync_IncludesSoftDeletedTombstones()
    {
        var id = Guid.NewGuid();
        var preset = MakePreset(id, "Gone");
        preset.IsDeleted = true;
        preset.DeletedAt = DateTime.UtcNow;
        await _sut.ApplyPresetAsync(preset);

        var all = await _sut.GetAllPresetsAsync();

        Assert.That(all.Single(p => p.Id == id).IsDeleted, Is.True);
    }

    [Test]
    public async Task MarkSyncedAsync_UpdatesBaseRevisionAndDirty()
    {
        var id = Guid.NewGuid();
        await _sut.ApplyPresetAsync(MakePreset(id, "Trains"));

        await _sut.MarkSyncedAsync(SyncEntityKind.Preset, id, 7, dirty: false);

        var preset = (await _sut.GetAllPresetsAsync()).Single(p => p.Id == id);
        Assert.Multiple(() =>
        {
            Assert.That(preset.BaseRevision, Is.EqualTo(7));
            Assert.That(preset.IsDirty, Is.False);
        });
    }

    private async Task<Guid> AddTombstoneAsync(DateTime deletedAt, bool dirty)
    {
        var id = Guid.NewGuid();
        var preset = MakePreset(id, "Tomb");
        preset.IsDeleted = true;
        preset.DeletedAt = deletedAt;
        preset.IsDirty = dirty;
        await _sut.ApplyPresetAsync(preset);
        return id;
    }

    [Test]
    public async Task PurgeTombstonesAsync_RemovesOldSyncedTombstone()
    {
        var id = await AddTombstoneAsync(DateTime.UtcNow.AddDays(-40), dirty: false);

        await _sut.PurgeTombstonesAsync(DateTime.UtcNow.AddDays(-30));

        Assert.That((await _sut.GetAllPresetsAsync()).Any(p => p.Id == id), Is.False);
    }

    [Test]
    public async Task PurgeTombstonesAsync_KeepsRecentTombstone()
    {
        var id = await AddTombstoneAsync(DateTime.UtcNow.AddDays(-1), dirty: false);

        await _sut.PurgeTombstonesAsync(DateTime.UtcNow.AddDays(-30));

        Assert.That((await _sut.GetAllPresetsAsync()).Any(p => p.Id == id), Is.True);
    }

    [Test]
    public async Task PurgeTombstonesAsync_KeepsUnpushedDirtyTombstone()
    {
        var id = await AddTombstoneAsync(DateTime.UtcNow.AddDays(-40), dirty: true);

        await _sut.PurgeTombstonesAsync(DateTime.UtcNow.AddDays(-30));

        Assert.That((await _sut.GetAllPresetsAsync()).Any(p => p.Id == id), Is.True);
    }
}
