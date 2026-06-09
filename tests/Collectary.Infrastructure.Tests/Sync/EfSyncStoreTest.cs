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
    public async Task GetReferencedImageKeysAsync_IncludesImageMultiImageDocumentAndAudioKeys()
    {
        var presetId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var multiId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var audioId = Guid.NewGuid();
        var preset = new Preset { Id = presetId, Name = "P", Revision = 1 };
        preset.Fields.Add(new ImageFieldDefinition { Id = imageId, Label = "Img", PresetId = presetId });
        preset.Fields.Add(new MultiImageFieldDefinition { Id = multiId, Label = "Gallery", PresetId = presetId });
        preset.Fields.Add(new FileAttachmentFieldDefinition { Id = fileId, Label = "Docs", PresetId = presetId });
        preset.Fields.Add(new AudioFieldDefinition { Id = audioId, Label = "Note", PresetId = presetId });
        await _sut.ApplyPresetAsync(preset);

        var itemId = Guid.NewGuid();
        var item = new Item { Id = itemId, DisplayName = "X", PresetId = presetId, Revision = 1 };
        item.Values.Add(new ImageFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = imageId, ItemId = itemId, ImageKey = "img-key" });
        item.Values.Add(new MultiImageFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = multiId, ItemId = itemId, ImageKeys = ["m1", "m2"] });
        item.Values.Add(new FileAttachmentFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = fileId, ItemId = itemId, Files = [new("doc-key", "manual.pdf")] });
        item.Values.Add(new AudioFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = audioId, ItemId = itemId, AudioKey = "aud-key" });
        await _sut.ApplyItemAsync(item);

        var keys = await _sut.GetReferencedImageKeysAsync();

        Assert.That(keys, Is.SupersetOf(new[] { "img-key", "m1", "m2", "doc-key", "aud-key" }));
    }

    [Test]
    public async Task ApplySharedFieldAsync_InsertsThenUpdatesInPlace()
    {
        var id = Guid.NewGuid();
        var sf = new SharedField { Id = id, Name = "Year", Definition = new IntegerFieldDefinition { Label = "Year" }, Revision = 1 };
        sf.Definition.SharedFieldId = id;
        await _sut.ApplySharedFieldAsync(sf);

        var updated = new SharedField { Id = id, Name = "Release year", Definition = new IntegerFieldDefinition { Label = "Year" }, Revision = 2 };
        updated.Definition.SharedFieldId = id;
        await _sut.ApplySharedFieldAsync(updated);

        var all = await _sut.GetAllSharedFieldsAsync();
        Assert.That(all.Where(x => x.Id == id).Select(x => x.Name), Is.EqualTo(new[] { "Release year" }));
    }

    [Test]
    public async Task ApplyUserAsync_InsertsThenUpdatesInPlace()
    {
        var id = Guid.NewGuid();
        var device = Guid.NewGuid();
        await _sut.ApplyUserAsync(new User { Id = id, Username = "alice", DisplayName = "Alice", Revision = 1 });
        await _sut.ApplyUserAsync(new User { Id = id, Username = "alice", DisplayName = "Alice B", Revision = 2, Lamport = 9, LastModifiedByDeviceId = device });

        var stored = (await _sut.GetAllUsersAsync()).Single(u => u.Id == id);
        Assert.Multiple(() =>
        {
            Assert.That(stored.DisplayName, Is.EqualTo("Alice B"));
            Assert.That(stored.Revision, Is.EqualTo(2), "the upsert copies sync metadata onto the tracked row");
            Assert.That(stored.Lamport, Is.EqualTo(9));
            Assert.That(stored.LastModifiedByDeviceId, Is.EqualTo(device));
        });
    }

    [Test]
    public async Task ApplyUserAsync_WhenUsernameCollidesWithDifferentUser_UniquifiesAndKeepsBoth()
    {
        await _sut.ApplyUserAsync(new User { Id = Guid.NewGuid(), Username = "alice", DisplayName = "A1", Revision = 1 });
        await _sut.ApplyUserAsync(new User { Id = Guid.NewGuid(), Username = "alice", DisplayName = "A2", Revision = 1 });

        var all = await _sut.GetAllUsersAsync();
        Assert.Multiple(() =>
        {
            Assert.That(all.Count(u => u.DisplayName is "A1" or "A2"), Is.EqualTo(2), "both colliding users survive");
            Assert.That(all.Select(u => u.Username).Distinct().Count(), Is.EqualTo(all.Count), "usernames are made unique");
        });
    }

    [Test]
    public async Task ApplyShareAsync_InsertsThenUpdatesInPlace()
    {
        var id = Guid.NewGuid();
        var presetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var grantedBy = Guid.NewGuid();
        var device = Guid.NewGuid();
        await _sut.ApplyShareAsync(new CollectionShare { Id = id, PresetId = presetId, SharedWithUserId = userId, GrantedByUserId = grantedBy, Permission = SharePermission.Read, Revision = 1 });
        await _sut.ApplyShareAsync(new CollectionShare { Id = id, PresetId = presetId, SharedWithUserId = userId, GrantedByUserId = grantedBy, Permission = SharePermission.Edit, Revision = 2, Lamport = 9, LastModifiedByDeviceId = device });

        var stored = (await _sut.GetAllSharesAsync()).Single(s => s.Id == id);
        Assert.Multiple(() =>
        {
            Assert.That(stored.Permission, Is.EqualTo(SharePermission.Edit));
            Assert.That(stored.Revision, Is.EqualTo(2), "the upsert copies sync metadata onto the tracked row");
            Assert.That(stored.Lamport, Is.EqualTo(9));
            Assert.That(stored.LastModifiedByDeviceId, Is.EqualTo(device));
        });
    }

    [Test]
    public async Task DeleteLocallyAsync_RemovesEntity()
    {
        var id = Guid.NewGuid();
        await _sut.ApplyPresetAsync(MakePreset(id, "Doomed"));

        await _sut.DeleteLocallyAsync(SyncEntityKind.Preset, id);

        Assert.That((await _sut.GetAllPresetsAsync()).Any(p => p.Id == id), Is.False);
    }

    [Test]
    public void DeleteLocallyAsync_WithUnknownKind_Throws()
    {
        Assert.That(async () => await _sut.DeleteLocallyAsync((SyncEntityKind)999, Guid.NewGuid()),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public async Task ApplyDeletionsAsync_HardDeletesRowAndRecordsTombstone()
    {
        var id = Guid.NewGuid();
        await _sut.ApplyPresetAsync(MakePreset(id, "Doomed"));

        await _sut.ApplyDeletionsAsync(new[] { id });

        var rows = await _sut.GetAllPresetsAsync();
        var tombstones = await _sut.GetTombstoneIdsAsync();
        Assert.Multiple(() =>
        {
            Assert.That(rows.Any(p => p.Id == id), Is.False, "the row is hard-deleted");
            Assert.That(tombstones, Does.Contain(id), "a permanent tombstone marker is recorded");
        });
    }

    [Test]
    public async Task ApplyDeletionsAsync_IsIdempotent_DoesNotDuplicateTombstones()
    {
        var id = Guid.NewGuid();
        await _sut.ApplyPresetAsync(MakePreset(id, "Doomed"));

        await _sut.ApplyDeletionsAsync(new[] { id });
        await _sut.ApplyDeletionsAsync(new[] { id });

        Assert.That((await _sut.GetTombstoneIdsAsync()).Count(t => t == id), Is.EqualTo(1));
    }

    [Test]
    public async Task StampPushedAsync_StampsLamportAndDeviceAndClearsDirty()
    {
        var id = Guid.NewGuid();
        var preset = MakePreset(id, "P");
        preset.IsDirty = true;
        await _sut.ApplyPresetAsync(preset);
        var device = Guid.NewGuid();

        await _sut.StampPushedAsync(SyncEntityKind.Preset, id, 42, device);

        var stored = (await _sut.GetAllPresetsAsync()).Single(p => p.Id == id);
        Assert.Multiple(() =>
        {
            Assert.That(stored.Lamport, Is.EqualTo(42));
            Assert.That(stored.LastModifiedByDeviceId, Is.EqualTo(device));
            Assert.That(stored.IsDirty, Is.False, "a pushed entity is no longer dirty");
        });
    }

    [Test]
    public async Task StampPushedAsync_WhenEntityMissing_LogsWarningAndDoesNotThrow()
    {
        var logger = new RecordingLogger();
        var sut = new EfSyncStore(DbFactory, new FieldDefinitionMerger(), logger);

        await sut.StampPushedAsync(SyncEntityKind.Item, Guid.NewGuid(), 1, Guid.NewGuid());

        Assert.That(logger.Warnings, Is.EqualTo(1));
    }

    [Test]
    public void EverySyncEntityKind_HasAnOpsMapEntry()
    {
        Assert.Multiple(() =>
        {
            foreach (var kind in Enum.GetValues<SyncEntityKind>())
                Assert.That(async () => await _sut.StampPushedAsync(kind, Guid.NewGuid(), 1, Guid.NewGuid()),
                    Throws.Nothing, $"{kind} must have an ops-map entry");
        });
    }

    [Test]
    public void StampPushedAsync_WithUnknownKind_Throws()
    {
        Assert.That(async () => await _sut.StampPushedAsync((SyncEntityKind)999, Guid.NewGuid(), 1, Guid.NewGuid()),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public async Task SyncState_PersistsHighWaterMonotonically()
    {
        Assert.That(await _sut.GetMaxObservedLamportAsync(), Is.EqualTo(0), "starts at zero");

        await _sut.SetMaxObservedLamportAsync(10);
        Assert.That(await _sut.GetMaxObservedLamportAsync(), Is.EqualTo(10));

        await _sut.SetMaxObservedLamportAsync(5);
        Assert.That(await _sut.GetMaxObservedLamportAsync(), Is.EqualTo(10), "a lower value never lowers the high-water mark");
    }
}
