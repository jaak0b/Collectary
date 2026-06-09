using Collectary.Core.Domain;
using Collectary.Core.Logging;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class SyncService : ISyncService
{
    public const string PresetKind = "presets";
    public const string ItemKind = "items";
    public const string SharedFieldKind = "sharedfields";
    public const string UserKind = "users";
    public const string ShareKind = "shares";
    public const string ImageKind = "images";
    public const string DevicesKind = "devices";

    private readonly ISyncBackend _backend;
    private readonly ISyncStore _store;
    private readonly ISyncSerializer _serializer;
    private readonly IDeviceIdentity _device;
    private readonly IImageStore? _imageStore;
    private readonly IAppLogger _logger;
    private readonly LamportClock _clock = new();
    private readonly SyncMergeEngine _merge;

    public SyncService(ISyncBackend backend, ISyncStore store, ISyncSerializer serializer, IDeviceIdentity device,
        ISyncStatus? syncStatus = null, IImageStore? imageStore = null, IAppLogger? logger = null)
    {
        _backend = backend;
        _store = store;
        _serializer = serializer;
        _device = device;
        _imageStore = imageStore;
        _logger = logger ?? new NullAppLogger();
        _merge = new SyncMergeEngine(_clock);
    }

    public async Task<SyncResult> SyncAsync()
    {
        if (!_backend.IsAvailable)
        {
            _logger.Warning("Sync skipped: the configured sync backend is not available");
            return new SyncResult(0, 0, Array.Empty<SyncConflict>());
        }

        _logger.Information("Sync starting");
        var myId = _device.DeviceId;
        var clock = await _store.GetMaxObservedLamportAsync();

        var users = await _store.GetAllUsersAsync();
        var sharedFields = await _store.GetAllSharedFieldsAsync();
        var presets = await _store.GetAllPresetsAsync();
        var items = await _store.GetAllItemsAsync();
        var shares = await _store.GetAllSharesAsync();
        var localTombstones = await _store.GetTombstoneIdsAsync();

        var pushed = 0;
        (pushed, clock) = await StampDirtyAsync(SyncEntityKind.User, users, myId, clock, pushed);
        (pushed, clock) = await StampDirtyAsync(SyncEntityKind.SharedField, sharedFields, myId, clock, pushed);
        (pushed, clock) = await StampDirtyAsync(SyncEntityKind.Preset, presets, myId, clock, pushed);
        (pushed, clock) = await StampDirtyAsync(SyncEntityKind.Item, items, myId, clock, pushed);
        (pushed, clock) = await StampDirtyAsync(SyncEntityKind.Share, shares, myId, clock, pushed);

        var mySnapshot = new DeviceSnapshot
        {
            DeviceId = myId,
            Users = users.ToList(),
            SharedFields = sharedFields.ToList(),
            Presets = presets.ToList(),
            Items = items.ToList(),
            Shares = shares.ToList(),
            Tombstones = localTombstones.ToList(),
        };
        await _backend.WriteAsync(DevicesKind, myId, _serializer.Serialize(mySnapshot), 0);

        var remoteSnapshots = await ReadRemoteSnapshotsAsync(myId);

        var deletedIds = new HashSet<Guid>(localTombstones);
        foreach (var snapshot in remoteSnapshots)
            deletedIds.UnionWith(snapshot.Tombstones);
        await _store.ApplyDeletionsAsync(deletedIds.ToList());

        var pulled = 0;
        var observed = clock;

        foreach (var (kindPulled, kindObserved) in new[]
        {
            await MergeKindAsync(remoteSnapshots.SelectMany(s => s.Users), users, deletedIds, _store.ApplyUserAsync),
            await MergeKindAsync(remoteSnapshots.SelectMany(s => s.SharedFields), sharedFields, deletedIds, _store.ApplySharedFieldAsync),
            await MergeKindAsync(remoteSnapshots.SelectMany(s => s.Presets), presets, deletedIds, _store.ApplyPresetAsync),
            await MergeKindAsync(remoteSnapshots.SelectMany(s => s.Items), items, deletedIds, _store.ApplyItemAsync),
            await MergeKindAsync(remoteSnapshots.SelectMany(s => s.Shares), shares, deletedIds, _store.ApplyShareAsync),
        })
        {
            pulled += kindPulled;
            observed = Math.Max(observed, kindObserved);
        }

        await _store.SetMaxObservedLamportAsync(observed);
        await SyncImagesAsync();

        _logger.Information("Sync complete: pushed={Pushed} pulled={Pulled}", pushed, pulled);
        return new SyncResult(pushed, pulled, Array.Empty<SyncConflict>());
    }

    public Task ResolveAsync(SyncConflict conflict, bool keepLocal) => Task.CompletedTask;

    private async Task<(int pushed, long clock)> StampDirtyAsync<T>(
        SyncEntityKind kind, IReadOnlyList<T> locals, Guid deviceId, long clock, int pushed)
        where T : DomainObject, ISyncable
    {
        foreach (var entity in locals.Where(e => e.IsDirty))
        {
            clock = _clock.Next(clock, entity.Lamport);
            entity.Lamport = clock;
            entity.LastModifiedByDeviceId = deviceId;
            await _store.StampPushedAsync(kind, entity.Id, clock, deviceId);
            pushed++;
        }

        return (pushed, clock);
    }

    private async Task<IReadOnlyList<DeviceSnapshot>> ReadRemoteSnapshotsAsync(Guid myId)
    {
        var entries = await _backend.ListAsync(DevicesKind);
        var snapshots = new List<DeviceSnapshot>();
        foreach (var entry in entries.Where(e => e.Id != myId))
        {
            var content = await _backend.ReadAsync(DevicesKind, entry.Id);
            if (content is null) continue;
            try
            {
                snapshots.Add(_serializer.Deserialize<DeviceSnapshot>(content));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Skipping unreadable device snapshot {DeviceId} during sync", entry.Id);
            }
        }

        return snapshots;
    }

    private async Task<(int pulled, long observed)> MergeKindAsync<T>(
        IEnumerable<T> remoteCandidates, IReadOnlyList<T> locals, ISet<Guid> deletedIds, Func<T, Task> apply)
        where T : DomainObject, ISyncable
    {
        var candidates = remoteCandidates
            .Select(e => new MergeCandidate<T>(e.Id, new SyncVersion(e.Lamport, e.LastModifiedByDeviceId), e))
            .ToList();
        long observed = 0;
        foreach (var candidate in candidates)
            observed = Math.Max(observed, candidate.Version.Lamport);

        var winners = _merge.ResolveWinners(candidates, deletedIds);
        var localVersions = locals.ToDictionary(l => l.Id, l => new SyncVersion(l.Lamport, l.LastModifiedByDeviceId));

        var pulled = 0;
        foreach (var winner in winners)
        {
            if (localVersions.TryGetValue(winner.Id, out var local) && _clock.Compare(winner.Version, local) <= 0)
                continue;

            ((ISyncable)winner.Payload).MarkPulled();
            try
            {
                await apply(winner.Payload);
                pulled++;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Skipping {Kind} {Id} that failed to apply locally during sync",
                    typeof(T).Name, winner.Id);
            }
        }

        return (pulled, observed);
    }

    private async Task SyncImagesAsync()
    {
        if (_imageStore is null) return;

        var referenced = (await _store.GetReferencedImageKeysAsync()).ToHashSet();
        var localSet = (await _imageStore.ListKeysAsync()).ToHashSet();
        var remoteSet = (await _backend.ListBlobKeysAsync(ImageKind)).ToHashSet();

        foreach (var key in referenced)
        {
            if (localSet.Contains(key) && !remoteSet.Contains(key))
            {
                using var stream = _imageStore.Open(key);
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);
                await _backend.WriteBlobAsync(ImageKind, key, buffer.ToArray());
            }
            else if (!localSet.Contains(key) && remoteSet.Contains(key))
            {
                var bytes = await _backend.ReadBlobAsync(ImageKind, key);
                if (bytes is null)
                {
                    _logger.Warning("Referenced image {Key} could not be downloaded; leaving it for the next sync", key);
                    continue;
                }
                using var stream = new MemoryStream(bytes);
                await _imageStore.ImportAsync(key, stream);
            }
        }

        foreach (var key in localSet.Where(k => !referenced.Contains(k)))
            await _imageStore.DeleteAsync(key);
    }
}
