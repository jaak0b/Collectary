using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
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
    public async Task SyncAsync_WhenPreviouslySyncedAbsentFromPopulatedRemote_DeletesLocally()
    {
        var p1 = DirtyPreset("Keep");
        var p2 = DirtyPreset("Gone");
        _storeA.Presets[p1.Id] = p1;
        _storeA.Presets[p2.Id] = p2;
        await _clientA.SyncAsync();
        await _clientB.SyncAsync();
        // p2's tombstone has been purged remotely (doc gone), remote still has p1
        await _backend.DeleteAsync(SyncService.PresetKind, p2.Id);

        await _clientB.SyncAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_storeB.Presets.ContainsKey(p2.Id), Is.False, "absent-from-populated-remote must be deleted locally");
            Assert.That(_storeB.Presets.ContainsKey(p1.Id), Is.True);
        });
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
}

internal sealed class TestSyncStatus : ISyncStatus
{
    public TestSyncStatus(int retentionDays) => TombstoneRetentionDays = retentionDays;
    public bool IsConfigured => true;
    public int TombstoneRetentionDays { get; }
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

    public Task<IReadOnlyList<string>> GetReferencedImageKeysAsync() =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

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
