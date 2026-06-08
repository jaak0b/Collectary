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
    public async Task ApplyPresetAsync_RemoteEdit_PreservesItemFieldValues()
    {
        var presetId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var preset = new Preset { Id = presetId, Name = "Trains", Revision = 1 };
        preset.Fields.Add(new TextFieldDefinition { Id = fieldId, Label = "Title", PresetId = presetId });
        await _sut.ApplyPresetAsync(preset);

        var itemId = Guid.NewGuid();
        var item = new Item { Id = itemId, DisplayName = "Loco", PresetId = presetId, Revision = 1 };
        item.Values.Add(new TextFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = fieldId, Value = "Flying Scotsman", ItemId = itemId });
        await _sut.ApplyItemAsync(item);

        var renamed = new Preset { Id = presetId, Name = "Locomotives", Revision = 2, BaseRevision = 2 };
        renamed.Fields.Add(new TextFieldDefinition { Id = fieldId, Label = "Title", PresetId = presetId });
        await _sut.ApplyPresetAsync(renamed);

        var values = (await _sut.GetAllItemsAsync()).Single(i => i.Id == itemId).Values;
        var presetName = (await _sut.GetAllPresetsAsync()).Single(p => p.Id == presetId).Name;
        Assert.Multiple(() =>
        {
            Assert.That(((TextFieldValue)values.Single()).Value, Is.EqualTo("Flying Scotsman"),
                "a preset re-apply must merge in place, not cascade-delete every item's field values");
            Assert.That(presetName, Is.EqualTo("Locomotives"));
        });
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
    public async Task GetReferencedImageKeysAsync_IncludesKeysFromSoftDeletedItems()
    {
        var presetId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var preset = new Preset { Id = presetId, Name = "P", Revision = 1 };
        preset.Fields.Add(new ImageFieldDefinition { Id = imageId, Label = "Img", PresetId = presetId });
        await _sut.ApplyPresetAsync(preset);

        var itemId = Guid.NewGuid();
        var item = new Item { Id = itemId, DisplayName = "X", PresetId = presetId, Revision = 1, IsDeleted = true, DeletedAt = DateTime.UtcNow };
        item.Values.Add(new ImageFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = imageId, ItemId = itemId, ImageKey = "tombstone-key" });
        await _sut.ApplyItemAsync(item);

        var keys = await _sut.GetReferencedImageKeysAsync();

        Assert.That(keys, Does.Contain("tombstone-key"));
    }

    [Test]
    public async Task GetLiveReferencedImageKeysAsync_ExcludesKeysFromSoftDeletedItems()
    {
        var presetId = Guid.NewGuid();
        var liveImg = Guid.NewGuid();
        var delImg = Guid.NewGuid();
        var preset = new Preset { Id = presetId, Name = "P", Revision = 1 };
        preset.Fields.Add(new ImageFieldDefinition { Id = liveImg, Label = "L", PresetId = presetId });
        preset.Fields.Add(new ImageFieldDefinition { Id = delImg, Label = "D", PresetId = presetId });
        await _sut.ApplyPresetAsync(preset);

        var liveId = Guid.NewGuid();
        var live = new Item { Id = liveId, DisplayName = "Live", PresetId = presetId, Revision = 1 };
        live.Values.Add(new ImageFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = liveImg, ItemId = liveId, ImageKey = "live-key" });
        await _sut.ApplyItemAsync(live);

        var delId = Guid.NewGuid();
        var del = new Item { Id = delId, DisplayName = "Gone", PresetId = presetId, Revision = 1, IsDeleted = true, DeletedAt = DateTime.UtcNow };
        del.Values.Add(new ImageFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = delImg, ItemId = delId, ImageKey = "deleted-key" });
        await _sut.ApplyItemAsync(del);

        var keys = await _sut.GetLiveReferencedImageKeysAsync();

        Assert.Multiple(() =>
        {
            Assert.That(keys, Does.Contain("live-key"));
            Assert.That(keys, Does.Not.Contain("deleted-key"));
        });
    }

    [Test]
    public async Task MarkSyncedAsync_WhenEntityMissing_LogsWarningAndDoesNotThrow()
    {
        var logger = new RecordingLogger();
        var sut = new EfSyncStore(DbFactory, new FieldDefinitionMerger(), logger);

        await sut.MarkSyncedAsync(SyncEntityKind.Item, Guid.NewGuid(), 1, dirty: false);

        Assert.That(logger.Warnings, Is.EqualTo(1), "a push whose local row vanished must be surfaced, not silently dropped");
    }

    [Test]
    public void MarkSyncedAsync_WithUnknownKind_Throws()
    {
        Assert.That(async () => await _sut.MarkSyncedAsync((SyncEntityKind)999, Guid.NewGuid(), 1, false),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void EverySyncEntityKind_HasAnOpsMapEntry()
    {
        Assert.Multiple(() =>
        {
            foreach (var kind in Enum.GetValues<SyncEntityKind>())
                Assert.That(async () => await _sut.MarkSyncedAsync(kind, Guid.NewGuid(), 1, dirty: false),
                    Throws.Nothing, $"{kind} must have an ops-map entry (adding an enum value without one must fail here)");
        });
    }

    [Test]
    public async Task MarkSyncedAsync_WhenLocalRevisionAdvancedPastPush_KeepsDirty()
    {
        var id = Guid.NewGuid();
        var preset = MakePreset(id, "Trains");
        preset.Revision = 6;
        preset.IsDirty = true;
        await _sut.ApplyPresetAsync(preset);

        await _sut.MarkSyncedAsync(SyncEntityKind.Preset, id, baseRevision: 5, dirty: false);

        var stored = (await _sut.GetAllPresetsAsync()).Single(p => p.Id == id);
        Assert.Multiple(() =>
        {
            Assert.That(stored.IsDirty, Is.True, "a concurrent edit (rev 6) past the pushed rev (5) must stay dirty so it re-pushes");
            Assert.That(stored.BaseRevision, Is.EqualTo(5));
            Assert.That(stored.Revision, Is.EqualTo(6));
        });
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
    public async Task DeleteLocallyAsync_RemovesItem()
    {
        var presetId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var preset = new Preset { Id = presetId, Name = "P", Revision = 1 };
        preset.Fields.Add(new TextFieldDefinition { Id = fieldId, Label = "Title", PresetId = presetId });
        await _sut.ApplyPresetAsync(preset);
        var itemId = Guid.NewGuid();
        var item = new Item { Id = itemId, DisplayName = "Gone", PresetId = presetId, Revision = 1 };
        item.Values.Add(new TextFieldValue { Id = Guid.NewGuid(), FieldDefinitionId = fieldId, Value = "x", ItemId = itemId });
        await _sut.ApplyItemAsync(item);

        await _sut.DeleteLocallyAsync(SyncEntityKind.Item, itemId);

        Assert.That((await _sut.GetAllItemsAsync()).Any(i => i.Id == itemId), Is.False);
    }

    [Test]
    public async Task DeleteLocallyAsync_RemovesPresetAndSharedField()
    {
        var presetId = Guid.NewGuid();
        await _sut.ApplyPresetAsync(MakePreset(presetId, "Gone"));
        var sfId = Guid.NewGuid();
        await _sut.ApplySharedFieldAsync(new SharedField { Id = sfId, Name = "Gone", Revision = 1, Definition = new TextFieldDefinition { SharedFieldId = sfId } });

        await _sut.DeleteLocallyAsync(SyncEntityKind.Preset, presetId);
        await _sut.DeleteLocallyAsync(SyncEntityKind.SharedField, sfId);

        var presetGone = (await _sut.GetAllPresetsAsync()).All(p => p.Id != presetId);
        var sharedGone = (await _sut.GetAllSharedFieldsAsync()).All(s => s.Id != sfId);
        Assert.Multiple(() =>
        {
            Assert.That(presetGone, Is.True);
            Assert.That(sharedGone, Is.True);
        });
    }

    [Test]
    public void DeleteLocallyAsync_WithUnknownKind_Throws()
    {
        Assert.That(async () => await _sut.DeleteLocallyAsync((SyncEntityKind)999, Guid.NewGuid()),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public async Task ApplySharedFieldAsync_InsertsThenUpdatesInPlace()
    {
        var id = Guid.NewGuid();
        await _sut.ApplySharedFieldAsync(new SharedField
        {
            Id = id, Name = "A", Revision = 1, Definition = new TextFieldDefinition { SharedFieldId = id },
        });
        await _sut.ApplySharedFieldAsync(new SharedField
        {
            Id = id, Name = "B", Revision = 2, Definition = new TextFieldDefinition { SharedFieldId = id },
        });

        var all = await _sut.GetAllSharedFieldsAsync();
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

    [Test]
    public async Task ApplyUserAsync_InsertsAndGetAllReturnsIt()
    {
        var id = Guid.NewGuid();
        await _sut.ApplyUserAsync(new User { Id = id, Username = "alice", DisplayName = "Alice", Revision = 1, BaseRevision = 1 });

        var all = await _sut.GetAllUsersAsync();

        var stored = all.Single(u => u.Id == id);
        Assert.Multiple(() =>
        {
            Assert.That(stored.Username, Is.EqualTo("alice"));
            Assert.That(stored.DisplayName, Is.EqualTo("Alice"));
            Assert.That(stored.Revision, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ApplyUserAsync_UpdatesInPlaceWithoutDuplicating()
    {
        var id = Guid.NewGuid();
        await _sut.ApplyUserAsync(new User { Id = id, Username = "alice", DisplayName = "Alice", Revision = 1 });
        await _sut.ApplyUserAsync(new User { Id = id, Username = "alice", DisplayName = "Alice Cooper", Revision = 2, BaseRevision = 2 });

        var all = await _sut.GetAllUsersAsync();

        Assert.Multiple(() =>
        {
            Assert.That(all.Count(u => u.Id == id), Is.EqualTo(1));
            Assert.That(all.Single(u => u.Id == id).DisplayName, Is.EqualTo("Alice Cooper"));
            Assert.That(all.Single(u => u.Id == id).Revision, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ApplyUserAsync_WhenUsernameCollidesWithDifferentUser_UniquifiesAndKeepsBoth()
    {
        var localId = Guid.NewGuid();
        var incomingId = Guid.NewGuid();
        await _sut.ApplyUserAsync(new User { Id = localId, Username = "alice", DisplayName = "Local Alice", Revision = 1 });

        await _sut.ApplyUserAsync(new User { Id = incomingId, Username = "Alice", DisplayName = "Remote Alice", Revision = 1 });

        var all = await _sut.GetAllUsersAsync();
        var local = all.Single(u => u.Id == localId);
        var incoming = all.Single(u => u.Id == incomingId);
        Assert.Multiple(() =>
        {
            Assert.That(all.Count(u => u.Id == localId || u.Id == incomingId), Is.EqualTo(2), "both profiles must coexist");
            Assert.That(local.Username, Is.EqualTo("alice"), "the local username is untouched");
            Assert.That(incoming.Username, Is.Not.EqualTo("alice").IgnoreCase, "the colliding incoming username is uniquified");
            Assert.That(incoming.DisplayName, Is.EqualTo("Remote Alice"), "the incoming display name is preserved");
            Assert.That(incoming.Id, Is.EqualTo(incomingId), "the incoming id is preserved so ownership references still resolve");
        });
    }

    [Test]
    public async Task ApplyUserAsync_WhenUniquifiedNameAlsoCollides_KeepsIncrementingTheCounter()
    {
        var incomingId = Guid.NewGuid();
        await _sut.ApplyUserAsync(new User { Id = Guid.NewGuid(), Username = "alice", DisplayName = "A", Revision = 1 });
        await _sut.ApplyUserAsync(new User { Id = Guid.NewGuid(), Username = "alice-2", DisplayName = "B", Revision = 1 });

        await _sut.ApplyUserAsync(new User { Id = incomingId, Username = "alice", DisplayName = "Incoming", Revision = 1 });

        var stored = (await _sut.GetAllUsersAsync()).Single(u => u.Id == incomingId);
        Assert.That(stored.Username, Is.EqualTo("alice-3"),
            "when both 'alice' and 'alice-2' are taken, the counter advances until the username is unique");
    }

    [Test]
    public async Task ApplyShareAsync_InsertsAndGetAllReturnsIt()
    {
        var id = Guid.NewGuid();
        var presetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await _sut.ApplyShareAsync(new CollectionShare
        {
            Id = id, PresetId = presetId, SharedWithUserId = userId, GrantedByUserId = Guid.NewGuid(),
            Permission = SharePermission.Edit, Revision = 1, BaseRevision = 1,
        });

        var all = await _sut.GetAllSharesAsync();

        var stored = all.Single(s => s.Id == id);
        Assert.Multiple(() =>
        {
            Assert.That(stored.PresetId, Is.EqualTo(presetId));
            Assert.That(stored.SharedWithUserId, Is.EqualTo(userId));
            Assert.That(stored.Permission, Is.EqualTo(SharePermission.Edit));
        });
    }

    [Test]
    public async Task ApplyShareAsync_UpdatesInPlaceWithoutDuplicating()
    {
        var id = Guid.NewGuid();
        var presetId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await _sut.ApplyShareAsync(new CollectionShare { Id = id, PresetId = presetId, SharedWithUserId = userId, GrantedByUserId = Guid.NewGuid(), Permission = SharePermission.Read, Revision = 1 });
        await _sut.ApplyShareAsync(new CollectionShare { Id = id, PresetId = presetId, SharedWithUserId = userId, GrantedByUserId = Guid.NewGuid(), Permission = SharePermission.Edit, Revision = 2, BaseRevision = 2 });

        var all = await _sut.GetAllSharesAsync();

        Assert.Multiple(() =>
        {
            Assert.That(all.Count(s => s.Id == id), Is.EqualTo(1));
            Assert.That(all.Single(s => s.Id == id).Permission, Is.EqualTo(SharePermission.Edit));
            Assert.That(all.Single(s => s.Id == id).Revision, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task MarkSyncedAsync_ForUserAndShare_UpdatesBaseRevisionAndDirty()
    {
        var userId = Guid.NewGuid();
        await _sut.ApplyUserAsync(new User { Id = userId, Username = "alice", DisplayName = "Alice", Revision = 3, IsDirty = true });
        var shareId = Guid.NewGuid();
        await _sut.ApplyShareAsync(new CollectionShare { Id = shareId, PresetId = Guid.NewGuid(), SharedWithUserId = Guid.NewGuid(), GrantedByUserId = Guid.NewGuid(), Revision = 3, IsDirty = true });

        await _sut.MarkSyncedAsync(SyncEntityKind.User, userId, 3, dirty: false);
        await _sut.MarkSyncedAsync(SyncEntityKind.Share, shareId, 3, dirty: false);

        var user = (await _sut.GetAllUsersAsync()).Single(u => u.Id == userId);
        var share = (await _sut.GetAllSharesAsync()).Single(s => s.Id == shareId);
        Assert.Multiple(() =>
        {
            Assert.That(user.BaseRevision, Is.EqualTo(3));
            Assert.That(user.IsDirty, Is.False);
            Assert.That(share.BaseRevision, Is.EqualTo(3));
            Assert.That(share.IsDirty, Is.False);
        });
    }

    [Test]
    public async Task DeleteLocallyAsync_RemovesUserAndShare()
    {
        var userId = Guid.NewGuid();
        await _sut.ApplyUserAsync(new User { Id = userId, Username = "gone", DisplayName = "Gone", Revision = 1 });
        var shareId = Guid.NewGuid();
        await _sut.ApplyShareAsync(new CollectionShare { Id = shareId, PresetId = Guid.NewGuid(), SharedWithUserId = Guid.NewGuid(), GrantedByUserId = Guid.NewGuid(), Revision = 1 });

        await _sut.DeleteLocallyAsync(SyncEntityKind.User, userId);
        await _sut.DeleteLocallyAsync(SyncEntityKind.Share, shareId);

        var users = await _sut.GetAllUsersAsync();
        var shares = await _sut.GetAllSharesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(users.Any(u => u.Id == userId), Is.False);
            Assert.That(shares.Any(s => s.Id == shareId), Is.False);
        });
    }

    [Test]
    public async Task PurgeTombstonesAsync_RemovesOldSyncedUserAndShareTombstone()
    {
        var userId = Guid.NewGuid();
        await _sut.ApplyUserAsync(new User { Id = userId, Username = "old", DisplayName = "Old", Revision = 2, BaseRevision = 2, IsDirty = false, IsDeleted = true, DeletedAt = DateTime.UtcNow.AddDays(-40) });
        var shareId = Guid.NewGuid();
        await _sut.ApplyShareAsync(new CollectionShare { Id = shareId, PresetId = Guid.NewGuid(), SharedWithUserId = Guid.NewGuid(), GrantedByUserId = Guid.NewGuid(), Revision = 2, BaseRevision = 2, IsDirty = false, IsDeleted = true, DeletedAt = DateTime.UtcNow.AddDays(-40) });

        await _sut.PurgeTombstonesAsync(DateTime.UtcNow.AddDays(-30));

        var users = await _sut.GetAllUsersAsync();
        var shares = await _sut.GetAllSharesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(users.Any(u => u.Id == userId), Is.False);
            Assert.That(shares.Any(s => s.Id == shareId), Is.False);
        });
    }
}
