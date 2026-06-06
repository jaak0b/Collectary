using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Infrastructure.Storage;
using Collectary.Infrastructure.Sync;

namespace Collectary.Infrastructure.Tests.Sync;

[TestFixture]
public class SyncServiceTest : FileSystemTestBase
{
    private SyncSerializer _serializer = null!;
    private FileSystemSyncBackend _backend = null!;
    private InMemorySyncStore _storeA = null!;
    private InMemorySyncStore _storeB = null!;
    private SyncService _clientA = null!;
    private SyncService _clientB = null!;

    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        _serializer = new SyncSerializer();
        _backend = new FileSystemSyncBackend(TempDir);
        _storeA = new InMemorySyncStore();
        _storeB = new InMemorySyncStore();
        _clientA = new SyncService(_backend, _storeA, _serializer);
        _clientB = new SyncService(_backend, _storeB, _serializer);
    }

    private Preset DirtyPreset(string name) =>
        new() { Name = name, Revision = 1, BaseRevision = 0, IsDirty = true };

    [Test]
    public void SharedFieldKind_UsesSharedFieldsWireString()
    {
        Assert.That(SyncService.SharedFieldKind, Is.EqualTo("sharedfields"));
    }

    [Test]
    public async Task SyncAsync_PushesDirtyLocalToBackend()
    {
        var preset = DirtyPreset("Trains");
        _storeA.Presets[preset.Id] = preset;

        var result = await _clientA.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Pushed, Is.EqualTo(1));
            Assert.That(preset.IsDirty, Is.False);
            Assert.That(preset.BaseRevision, Is.EqualTo(1));
        });
        Assert.That(await _backend.ReadAsync(SyncService.PresetKind, preset.Id), Is.Not.Null);
    }

    [Test]
    public async Task SyncAsync_PullsRemoteToEmptyClient()
    {
        var preset = DirtyPreset("Trains");
        _storeA.Presets[preset.Id] = preset;
        await _clientA.SyncAsync();

        var result = await _clientB.SyncAsync();

        Assert.That(result.Pulled, Is.EqualTo(1));
        Assert.That(_storeB.Presets[preset.Id].Name, Is.EqualTo("Trains"));
        Assert.That(_storeB.Presets[preset.Id].IsDirty, Is.False);
        Assert.That(_storeB.Presets[preset.Id].BaseRevision, Is.EqualTo(1));
    }

    [Test]
    public async Task SyncAsync_WhenNothingChanged_IsNoOp()
    {
        var preset = DirtyPreset("Trains");
        _storeA.Presets[preset.Id] = preset;
        await _clientA.SyncAsync();

        var result = await _clientA.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Pushed, Is.EqualTo(0));
            Assert.That(result.Pulled, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task SyncAsync_WhenBackendUnavailable_ReturnsEmpty()
    {
        var offline = new SyncService(new FileSystemSyncBackend("  "), _storeA, _serializer);

        var result = await offline.SyncAsync();

        Assert.That(result.Pushed + result.Pulled, Is.EqualTo(0));
        Assert.That(result.HasConflicts, Is.False);
    }

    private async Task<Guid> EstablishSharedPresetAsync()
    {
        var preset = DirtyPreset("Orig");
        _storeA.Presets[preset.Id] = preset;
        await _clientA.SyncAsync();
        await _clientB.SyncAsync();
        return preset.Id;
    }

    [Test]
    public async Task SyncAsync_WhenBothEditedSameAggregate_ReportsConflict()
    {
        var id = await EstablishSharedPresetAsync();

        var a = _storeA.Presets[id];
        a.Name = "A-edit"; a.Revision = 2; a.IsDirty = true;
        await _clientA.SyncAsync();

        var b = _storeB.Presets[id];
        b.Name = "B-edit"; b.Revision = 2; b.IsDirty = true;
        var result = await _clientB.SyncAsync();

        Assert.That(result.HasConflicts, Is.True);
        Assert.That(result.Conflicts.Single().Id, Is.EqualTo(id));
        Assert.That(result.Conflicts.Single().LocalLabel, Is.EqualTo("B-edit"));
        Assert.That(result.Conflicts.Single().RemoteLabel, Is.EqualTo("A-edit"));
    }

    [Test]
    public async Task ResolveAsync_KeepLocal_PushesLocalOnNextSync()
    {
        var id = await EstablishSharedPresetAsync();
        var a = _storeA.Presets[id];
        a.Name = "A-edit"; a.Revision = 2; a.IsDirty = true;
        await _clientA.SyncAsync();
        var b = _storeB.Presets[id];
        b.Name = "B-edit"; b.Revision = 2; b.IsDirty = true;
        var conflict = (await _clientB.SyncAsync()).Conflicts.Single();

        await _clientB.ResolveAsync(conflict, keepLocal: true);
        await _clientB.SyncAsync();

        var remote = _serializer.Deserialize<Preset>((await _backend.ReadAsync(SyncService.PresetKind, id))!);
        Assert.That(remote.Name, Is.EqualTo("B-edit"));
    }

    [Test]
    public async Task ResolveAsync_KeepRemote_OverwritesLocal()
    {
        var id = await EstablishSharedPresetAsync();
        var a = _storeA.Presets[id];
        a.Name = "A-edit"; a.Revision = 2; a.IsDirty = true;
        await _clientA.SyncAsync();
        var b = _storeB.Presets[id];
        b.Name = "B-edit"; b.Revision = 2; b.IsDirty = true;
        var conflict = (await _clientB.SyncAsync()).Conflicts.Single();

        await _clientB.ResolveAsync(conflict, keepLocal: false);

        Assert.That(_storeB.Presets[id].Name, Is.EqualTo("A-edit"));
        Assert.That(_storeB.Presets[id].IsDirty, Is.False);
    }

    [Test]
    public async Task SyncAsync_WhenRemoteTombstoneWithNoLocalCopy_DoesNotMaterializeIt()
    {
        var id = Guid.NewGuid();
        var tomb = new Preset { Id = id, Name = "Ghost", Revision = 2, IsDeleted = true, DeletedAt = DateTime.UtcNow };
        await _backend.WriteAsync(SyncService.PresetKind, id, _serializer.Serialize(tomb), 2);

        await _clientB.SyncAsync();

        Assert.That(_storeB.Presets.ContainsKey(id), Is.False,
            "a remote tombstone for an id this device never had must not be inserted as a phantom row");
    }

    [Test]
    public async Task SyncAsync_Conflict_RecordsAuthoritativeListingRevision()
    {
        var id = await EstablishSharedPresetAsync();
        var a = _storeA.Presets[id]; a.Name = "A-edit"; a.Revision = 2; a.IsDirty = true;
        await _clientA.SyncAsync();

        var remote = _serializer.Deserialize<Preset>((await _backend.ReadAsync(SyncService.PresetKind, id))!);
        remote.Revision = 1;
        await _backend.WriteAsync(SyncService.PresetKind, id, _serializer.Serialize(remote), 5);
        var b = _storeB.Presets[id]; b.Name = "B-edit"; b.Revision = 2; b.IsDirty = true;

        var conflict = (await _clientB.SyncAsync()).Conflicts.Single();

        Assert.That(conflict.RemoteRevision, Is.EqualTo(5),
            "the conflict must record the authoritative listing revision, not the document body revision");
    }

    [Test]
    public async Task ResolveAsync_KeepLocal_DoesNotRewindRevisionBelowLocal()
    {
        var id = Guid.NewGuid();
        _storeA.Presets[id] = new Preset { Id = id, Name = "x", Revision = 7, BaseRevision = 1, IsDirty = true };
        var conflict = new SyncConflict(SyncEntityKind.Preset, id, "local", "remote", LocalRevision: 7, RemoteRevision: 2);

        await _clientA.ResolveAsync(conflict, keepLocal: true);

        Assert.That(_storeA.Presets[id].Revision, Is.EqualTo(8),
            "keep-local must advance past the higher local revision, never rewind to remote+1");
    }

    [Test]
    public async Task SyncAsync_PurgesExpiredSharedFieldTombstone()
    {
        var sfId = Guid.NewGuid();
        var sf = new SharedField { Id = sfId, Name = "Gone", Revision = 2, BaseRevision = 2, IsDirty = false, IsDeleted = true, DeletedAt = DateTime.UtcNow.AddDays(-400), Definition = new Collectary.Core.Domain.Fields.TextFieldDefinition { SharedFieldId = sfId } };
        _storeA.SharedFields[sf.Id] = sf;
        await _backend.WriteAsync(SyncService.SharedFieldKind, sf.Id, _serializer.Serialize(sf), 2);
        var service = new SyncService(_backend, _storeA, _serializer);

        await service.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_storeA.SharedFields.ContainsKey(sf.Id), Is.False);
            Assert.That(_backend.ReadAsync(SyncService.SharedFieldKind, sf.Id).Result, Is.Null, "the remote shared-field tombstone document is deleted via its kind");
        });
    }

    [Test]
    public async Task SyncAsync_WithoutSyncStatus_StillPurgesExpiredTombstones()
    {
        var p = new Preset { Id = Guid.NewGuid(), Name = "Old", Revision = 2, BaseRevision = 2, IsDirty = false, IsDeleted = true, DeletedAt = DateTime.UtcNow.AddDays(-400) };
        _storeA.Presets[p.Id] = p;
        await _backend.WriteAsync(SyncService.PresetKind, p.Id, _serializer.Serialize(p), 2);
        var service = new SyncService(_backend, _storeA, _serializer);

        await service.SyncAsync();

        Assert.That(_storeA.Presets.ContainsKey(p.Id), Is.False,
            "expired tombstones must be purged even when no ISyncStatus is configured");
    }

    [Test]
    public async Task SyncAsync_PropagatesTombstone()
    {
        var id = await EstablishSharedPresetAsync();
        var a = _storeA.Presets[id];
        a.IsDeleted = true; a.DeletedAt = DateTime.UtcNow; a.Revision = 2; a.IsDirty = true;
        await _clientA.SyncAsync();

        await _clientB.SyncAsync();

        Assert.That(_storeB.Presets[id].IsDeleted, Is.True);
    }

    [Test]
    public async Task SyncAsync_WhenRemoteRevisionAdvances_PullsRemote()
    {
        var id = await EstablishSharedPresetAsync();
        var remote = _serializer.Deserialize<Preset>((await _backend.ReadAsync(SyncService.PresetKind, id))!);
        remote.Name = "Diverged";
        remote.Revision = 2;
        await _backend.WriteAsync(SyncService.PresetKind, id, _serializer.Serialize(remote), remote.Revision);

        await _clientB.SyncAsync();

        Assert.That(_storeB.Presets[id].Name, Is.EqualTo("Diverged"));
    }

    [Test]
    public async Task SyncAsync_WhenPreviouslySyncedAbsentFromPopulatedRemote_KeepsLocal()
    {
        var p1 = DirtyPreset("Keep");
        var p2 = DirtyPreset("AlsoKeep");
        _storeA.Presets[p1.Id] = p1;
        _storeA.Presets[p2.Id] = p2;
        await _clientA.SyncAsync();
        await _clientB.SyncAsync();
        // p2's document is absent from the remote (different/restored folder, partial listing, etc.).
        // Absence is NOT deletion — a real delete arrives as a tombstone document, so the local copy must survive.
        await _backend.DeleteAsync(SyncService.PresetKind, p2.Id);

        await _clientB.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_storeB.Presets.ContainsKey(p2.Id), Is.True, "absence must never be treated as deletion");
            Assert.That(_storeB.Presets.ContainsKey(p1.Id), Is.True);
        });
    }

    [Test]
    public async Task SyncAsync_WhenRemoteTombstonePresent_SoftDeletesLocally()
    {
        var id = await EstablishSharedPresetAsync();
        var remote = _serializer.Deserialize<Preset>((await _backend.ReadAsync(SyncService.PresetKind, id))!);
        remote.IsDeleted = true; remote.DeletedAt = DateTime.UtcNow; remote.Revision = 2;
        await _backend.WriteAsync(SyncService.PresetKind, id, _serializer.Serialize(remote), remote.Revision);

        await _clientB.SyncAsync();

        Assert.That(_storeB.Presets[id].IsDeleted, Is.True, "a real deletion propagates as a tombstone document, not as absence");
    }

    [Test]
    public async Task SyncAsync_WhenRemoteKindEmpty_DoesNotDeleteLocal()
    {
        var p1 = DirtyPreset("Lonely");
        _storeA.Presets[p1.Id] = p1;
        await _clientA.SyncAsync();
        await _clientB.SyncAsync();
        // entire remote presets folder becomes empty (unavailable / not yet downloaded)
        await _backend.DeleteAsync(SyncService.PresetKind, p1.Id);

        await _clientB.SyncAsync();

        Assert.That(_storeB.Presets.ContainsKey(p1.Id), Is.True, "an empty remote must never trigger local deletion");
    }

    [Test]
    public async Task SyncAsync_WhenRemoteRevisionOlderThanBase_DoesNotClobberLocal()
    {
        var id = await EstablishSharedPresetAsync();
        var a = _storeA.Presets[id];
        a.Name = "A-v2"; a.Revision = 2; a.IsDirty = true;
        await _clientA.SyncAsync();

        var stale = _serializer.Deserialize<Preset>((await _backend.ReadAsync(SyncService.PresetKind, id))!);
        stale.Name = "Rolled-back"; stale.Revision = 1;
        await _backend.WriteAsync(SyncService.PresetKind, id, _serializer.Serialize(stale), 1);

        await _clientA.SyncAsync();

        Assert.That(_storeA.Presets[id].Name, Is.EqualTo("A-v2"),
            "a lower-revision remote must never overwrite newer local content");
    }

    [Test]
    public void ResolveAsync_WithUnknownKind_Throws()
    {
        var conflict = new SyncConflict((SyncEntityKind)999, Guid.NewGuid(), "a", "b", 1, 1);

        Assert.That(async () => await _clientA.ResolveAsync(conflict, keepLocal: false),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public async Task SyncAsync_WhenTombstonePurged_DeletesBackendDocument()
    {
        var service = new SyncService(_backend, _storeA, _serializer, new TestSyncStatus(retentionDays: 30));
        var p = new Preset
        {
            Name = "Old",
            Revision = 2,
            BaseRevision = 2,
            IsDirty = false,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-40),
        };
        _storeA.Presets[p.Id] = p;
        await _backend.WriteAsync(SyncService.PresetKind, p.Id, _serializer.Serialize(p), p.Revision);

        await service.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_storeA.Presets.ContainsKey(p.Id), Is.False, "purged locally");
            Assert.That(_backend.ReadAsync(SyncService.PresetKind, p.Id).Result, Is.Null, "backend doc deleted");
        });
    }

    [Test]
    public async Task SyncAsync_WhenBothEditedButRemoteUnreadable_SurfacesConflictNotSilentStrand()
    {
        var id = await EstablishSharedPresetAsync();
        var a = _storeA.Presets[id];
        a.Name = "A-edit"; a.Revision = 2; a.IsDirty = true;
        await _clientA.SyncAsync();

        var b = _storeB.Presets[id];
        b.Name = "B-edit"; b.Revision = 2; b.IsDirty = true;
        var clientB = new SyncService(_backend, _storeB, new FlakySerializer { PoisonMarker = "A-edit" });

        var result = await clientB.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.HasConflicts, Is.True,
                "an unreadable remote in a both-changed state must surface a conflict, not silently strand the local edit");
            Assert.That(result.Conflicts.Single().Id, Is.EqualTo(id));
            Assert.That(_storeB.Presets[id].IsDirty, Is.True, "the local edit must remain pending, not be lost");
        });
    }

    [Test]
    public async Task SyncAsync_WhenRemoteHasDuplicateRevisionFiles_DoesNotThrowAndTakesHighest()
    {
        var id = Guid.NewGuid();
        var dir = Path.Combine(TempDir, SyncService.PresetKind);
        Directory.CreateDirectory(dir);
        var json5 = _serializer.Serialize(new Preset { Id = id, Name = "Old", Revision = 5 });
        var json6 = _serializer.Serialize(new Preset { Id = id, Name = "Dup", Revision = 6 });
        await File.WriteAllTextAsync(Path.Combine(dir, $"{id:N}.5.json"), json5);
        await File.WriteAllTextAsync(Path.Combine(dir, $"{id:N}.6.json"), json6);

        var result = await _clientB.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.HasConflicts, Is.False);
            Assert.That(_storeB.Presets[id].Name, Is.EqualTo("Dup"),
                "duplicate revision files must collapse to the highest and never abort sync");
        });
    }

    [Test]
    public async Task SyncAsync_WhenConflictPresent_DoesNotDeleteRemoteBlobs()
    {
        var id = await EstablishSharedPresetAsync();
        var a = _storeA.Presets[id]; a.Name = "A-edit"; a.Revision = 2; a.IsDirty = true;
        await _clientA.SyncAsync();
        var b = _storeB.Presets[id]; b.Name = "B-edit"; b.Revision = 2; b.IsDirty = true;

        await _backend.WriteBlobAsync(SyncService.ImageKind, "remote-only", new byte[] { 1, 2, 3 });
        var images = new FileSystemImageStore(Path.Combine(TempDir, "imgconflict"));
        var clientB = new SyncService(_backend, _storeB, _serializer, imageStore: images);

        var result = await clientB.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.HasConflicts, Is.True);
            Assert.That(_backend.ReadBlobAsync(SyncService.ImageKind, "remote-only").Result, Is.Not.Null,
                "an incomplete reconcile (conflict) must not GC remote blobs from this device's partial view");
        });
    }

    [Test]
    public async Task SyncAsync_WhenReconcileClean_DeletesUnreferencedRemoteBlob()
    {
        await _backend.WriteBlobAsync(SyncService.ImageKind, "orphan", new byte[] { 9 });
        var images = new FileSystemImageStore(Path.Combine(TempDir, "imgclean"));
        var clientB = new SyncService(_backend, _storeB, _serializer, imageStore: images);

        await clientB.SyncAsync();

        Assert.That(_backend.ReadBlobAsync(SyncService.ImageKind, "orphan").Result, Is.Null,
            "a clean reconcile with the full reference picture GCs genuinely-orphaned remote blobs");
    }

    [Test]
    public async Task SyncAsync_WhenReferencedRemoteImageFailsToDownload_LogsAndLeavesIt()
    {
        var images = new FileSystemImageStore(Path.Combine(TempDir, "imgnull"));
        _storeB.ReferencedImageKeys.Add("remote-key");
        var logger = new RecordingLogger();
        var service = new SyncService(new NullDownloadBackend("remote-key"), _storeB, _serializer, imageStore: images, logger: logger);

        await service.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(images.Exists("remote-key"), Is.False, "a failed download must not be imported as if it succeeded");
            Assert.That(logger.Warnings, Is.GreaterThanOrEqualTo(1), "a referenced image that cannot be downloaded must be logged");
        });
    }

    [Test]
    public async Task SyncAsync_WhenReferencedImageMissingEverywhere_DoesNotThrow()
    {
        var images = new FileSystemImageStore(Path.Combine(TempDir, "imgghost"));
        _storeB.ReferencedImageKeys.Add("ghost");

        var clientB = new SyncService(_backend, _storeB, _serializer, imageStore: images);

        Assert.Multiple(() =>
        {
            Assert.That(async () => await clientB.SyncAsync(), Throws.Nothing);
            Assert.That(images.Exists("ghost"), Is.False);
        });
    }

    [Test]
    public async Task SyncAsync_KeepsLocalImagesReferencedByTombstones()
    {
        var images = new FileSystemImageStore(Path.Combine(TempDir, "imgtomb"));
        using (var ms = new MemoryStream(new byte[] { 1, 2 })) await images.ImportAsync("tomb-key", ms);
        _storeB.ReferencedImageKeys.Add("tomb-key");

        var clientB = new SyncService(_backend, _storeB, _serializer, imageStore: images);
        await clientB.SyncAsync();

        Assert.That(images.Exists("tomb-key"), Is.True,
            "image GC must use the tombstone-inclusive referenced set so a soft-deleted item's image survives until purge");
    }

    [Test]
    public async Task SyncAsync_WhenOneRemoteDocumentCannotDeserialize_StillSyncsTheRest()
    {
        var poison = DirtyPreset("Poison");
        var good = DirtyPreset("Good");
        _storeA.Presets[poison.Id] = poison;
        _storeA.Presets[good.Id] = good;
        await _clientA.SyncAsync();

        var flaky = new FlakySerializer { PoisonMarker = "Poison" };
        var logger = new RecordingLogger();
        var clientB = new SyncService(_backend, _storeB, flaky, logger: logger);

        await clientB.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_storeB.Presets.ContainsKey(good.Id), Is.True, "a healthy document must still sync");
            Assert.That(_storeB.Presets.ContainsKey(poison.Id), Is.False, "an un-deserializable document is skipped, not fatal");
            Assert.That(logger.Errors, Is.EqualTo(1), "the skipped document is logged via the injected logger");
        });
    }
}

internal sealed class RecordingLogger : IAppLogger
{
    public int Errors { get; private set; }
    public int Warnings { get; private set; }
    public void Verbose(string messageTemplate, params object?[] propertyValues) { }
    public void Debug(string messageTemplate, params object?[] propertyValues) { }
    public void Information(string messageTemplate, params object?[] propertyValues) { }
    public void Warning(string messageTemplate, params object?[] propertyValues) => Warnings++;
    public void Error(Exception exception, string messageTemplate, params object?[] propertyValues) => Errors++;
}

internal sealed class FlakySerializer : ISyncSerializer
{
    private readonly SyncSerializer _inner = new();

    public string PoisonMarker { get; set; } = "";

    public string Serialize<T>(T value) => _inner.Serialize(value);

    public T Deserialize<T>(string json) =>
        PoisonMarker.Length > 0 && json.Contains(PoisonMarker)
            ? throw new InvalidOperationException("corrupt document")
            : _inner.Deserialize<T>(json);
}

internal sealed class TestSyncStatus : ISyncStatus
{
    public TestSyncStatus(int retentionDays) => TombstoneRetentionDays = retentionDays;
    public bool IsConfigured => true;
    public int TombstoneRetentionDays { get; }
}


internal sealed class NullDownloadBackend : ISyncBackend
{
    private readonly string _key;
    public NullDownloadBackend(string key) => _key = key;
    public bool IsAvailable => true;
    public Task<IReadOnlyList<SyncEntry>> ListAsync(string kind) => Task.FromResult<IReadOnlyList<SyncEntry>>(Array.Empty<SyncEntry>());
    public Task<string?> ReadAsync(string kind, Guid id) => Task.FromResult<string?>(null);
    public Task WriteAsync(string kind, Guid id, string content, long revision) => Task.CompletedTask;
    public Task DeleteAsync(string kind, Guid id) => Task.CompletedTask;
    public Task<IReadOnlyList<string>> ListBlobKeysAsync(string kind) => Task.FromResult<IReadOnlyList<string>>(new[] { _key });
    public Task<byte[]?> ReadBlobAsync(string kind, string key) => Task.FromResult<byte[]?>(null);
    public Task WriteBlobAsync(string kind, string key, byte[] content) => Task.CompletedTask;
    public Task DeleteBlobAsync(string kind, string key) => Task.CompletedTask;
}

internal sealed class InMemorySyncStore : ISyncStore
{
    public Dictionary<Guid, Preset> Presets { get; } = new();
    public Dictionary<Guid, Item> Items { get; } = new();
    public Dictionary<Guid, SharedField> SharedFields { get; } = new();

    public Task<IReadOnlyList<Preset>> GetAllPresetsAsync() =>
        Task.FromResult<IReadOnlyList<Preset>>(Presets.Values.ToList());

    public Task<IReadOnlyList<Item>> GetAllItemsAsync() =>
        Task.FromResult<IReadOnlyList<Item>>(Items.Values.ToList());

    public Task<IReadOnlyList<SharedField>> GetAllSharedFieldsAsync() =>
        Task.FromResult<IReadOnlyList<SharedField>>(SharedFields.Values.ToList());

    public Task ApplyPresetAsync(Preset preset)
    {
        Presets[preset.Id] = preset;
        return Task.CompletedTask;
    }

    public Task ApplyItemAsync(Item item)
    {
        Items[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task ApplySharedFieldAsync(SharedField sharedField)
    {
        SharedFields[sharedField.Id] = sharedField;
        return Task.CompletedTask;
    }

    public Task MarkSyncedAsync(SyncEntityKind kind, Guid id, long baseRevision, bool dirty, long? revision = null)
    {
        ISyncable? target = kind switch
        {
            SyncEntityKind.Preset => Presets.GetValueOrDefault(id),
            SyncEntityKind.Item => Items.GetValueOrDefault(id),
            _ => SharedFields.GetValueOrDefault(id),
        };
        if (target is null) return Task.CompletedTask;

        target.BaseRevision = baseRevision;
        target.IsDirty = dirty;
        if (revision.HasValue) target.Revision = revision.Value;
        return Task.CompletedTask;
    }

    public int PurgeCalls { get; private set; }

    public Task<IReadOnlyList<PurgedTombstone>> PurgeTombstonesAsync(DateTime cutoff)
    {
        PurgeCalls++;
        var purged = new List<PurgedTombstone>();
        PurgeKind(Presets, SyncEntityKind.Preset, cutoff, purged);
        PurgeKind(Items, SyncEntityKind.Item, cutoff, purged);
        PurgeKind(SharedFields, SyncEntityKind.SharedField, cutoff, purged);
        return Task.FromResult<IReadOnlyList<PurgedTombstone>>(purged);
    }

    private static void PurgeKind<T>(Dictionary<Guid, T> store, SyncEntityKind kind, DateTime cutoff, List<PurgedTombstone> purged)
        where T : ISyncable
    {
        foreach (var (id, value) in store.ToList())
        {
            if (value.IsDeleted && !value.IsDirty && value.DeletedAt is { } d && d < cutoff)
            {
                store.Remove(id);
                purged.Add(new PurgedTombstone(kind, id));
            }
        }
    }

    public List<string> ReferencedImageKeys { get; } = new();

    public Task<IReadOnlyList<string>> GetReferencedImageKeysAsync() =>
        Task.FromResult<IReadOnlyList<string>>(ReferencedImageKeys.ToList());

    public Task<IReadOnlyList<string>> GetLiveReferencedImageKeysAsync() =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

    public Task DeleteLocallyAsync(SyncEntityKind kind, Guid id)
    {
        switch (kind)
        {
            case SyncEntityKind.Preset: Presets.Remove(id); break;
            case SyncEntityKind.Item: Items.Remove(id); break;
            case SyncEntityKind.SharedField: SharedFields.Remove(id); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sync entity kind");
        }
        return Task.CompletedTask;
    }
}
