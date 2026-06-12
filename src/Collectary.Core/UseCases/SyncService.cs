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
    private readonly SnapshotIntegrity _integrity = new();
    private readonly SyncKindCatalog _catalog = new();

    public SyncService(ISyncBackend backend, ISyncStore store, ISyncSerializer serializer, IDeviceIdentity device,
        IImageStore? imageStore = null, IAppLogger? logger = null)
    {
        _backend = backend;
        _store = store;
        _serializer = serializer;
        _device = device;
        _imageStore = imageStore;
        _logger = logger ?? new NullAppLogger();
        _merge = new SyncMergeEngine();
    }

    public async Task<SyncResult> SyncAsync()
    {
        if (!_backend.IsAvailable)
        {
            _logger.Warning("Sync skipped: the configured sync backend is not available");
            return new SyncResult(0, 0, BackendUnavailable: true);
        }

        _logger.Information("Sync starting");
        var myId = _device.DeviceId;
        var kinds = _catalog.Describe(_store, _serializer);

        var localTombstones = await _store.GetTombstoneIdsAsync();
        var raw = await ReadPeerFilesAsync(myId);
        var fingerprint = TryComputeFingerprint(raw.Peers, localTombstones.Count);

        if (await NothingChangedSinceLastSyncAsync(raw, fingerprint))
        {
            _logger.Information("Sync idle: nothing changed since the last sync");
            return new SyncResult(0, 0);
        }

        var remote = DeserializePeers(raw.Peers);
        var locals = await LoadLocalEntitiesAsync(kinds);
        var stamped = await StampDirtyEntitiesAsync(kinds, locals, myId);
        var deletedIds = await ApplyDeleteWinsAsync(localTombstones, remote.Snapshots);
        await WriteOwnSnapshotAsync(kinds, locals, deletedIds, myId);
        var merged = await MergeRemotesAsync(kinds, remote.Snapshots, locals, deletedIds, stamped.Clock);

        await _store.SetMaxObservedLamportAsync(merged.Observed);
        var imagesFailed = await SyncImagesAsync();

        var unreadable = raw.Unreadable + remote.Unreadable;
        if (fingerprint is not null && unreadable == 0)
            await _store.SetSyncFingerprintAsync(fingerprint);

        _logger.Information(
            "Sync complete: pushed={Pushed} pulled={Pulled} skipped={Skipped} unreadableDevices={Unreadable} imagesFailed={ImagesFailed}",
            stamped.Pushed, merged.Pulled, merged.Skipped, unreadable, imagesFailed);
        return new SyncResult(stamped.Pushed, merged.Pulled, merged.Skipped, unreadable, imagesFailed);
    }

    private async Task<bool> NothingChangedSinceLastSyncAsync(RawPeers raw, string? fingerprint) =>
        raw.OwnPresent
        && raw.Unreadable == 0
        && fingerprint is not null
        && fingerprint == await _store.GetSyncFingerprintAsync()
        && !await _store.HasDirtyEntitiesAsync();

    private async Task<Dictionary<SyncEntityKind, IReadOnlyList<ISyncable>>> LoadLocalEntitiesAsync(
        IReadOnlyList<SyncKind> kinds)
    {
        var locals = new Dictionary<SyncEntityKind, IReadOnlyList<ISyncable>>();
        foreach (var kind in kinds)
            locals[kind.Kind] = await kind.GetLocal();
        return locals;
    }

    private readonly record struct StampOutcome(int Pushed, long Clock);

    private async Task<StampOutcome> StampDirtyEntitiesAsync(
        IReadOnlyList<SyncKind> kinds, Dictionary<SyncEntityKind, IReadOnlyList<ISyncable>> locals, Guid myId)
    {
        var clock = await _store.GetMaxObservedLamportAsync();
        var stamps = new List<PushStamp>();
        foreach (var kind in kinds)
            foreach (var entity in locals[kind.Kind].Where(e => e.IsDirty))
            {
                clock = _clock.Next(clock, entity.Lamport);
                entity.StampLamport(clock, myId);
                stamps.Add(new PushStamp(kind.Kind, entity.Id, clock, myId));
            }

        await _store.StampPushedAsync(stamps);
        return new StampOutcome(stamps.Count, clock);
    }

    private async Task<HashSet<Guid>> ApplyDeleteWinsAsync(
        IReadOnlyList<Guid> localTombstones, IReadOnlyList<DeviceSnapshot> remoteSnapshots)
    {
        var deletedIds = new HashSet<Guid>(localTombstones);
        foreach (var snapshot in remoteSnapshots)
            deletedIds.UnionWith(snapshot.Tombstones);
        await _store.ApplyDeletionsAsync(deletedIds.ToList());
        return deletedIds;
    }

    private async Task WriteOwnSnapshotAsync(
        IReadOnlyList<SyncKind> kinds, Dictionary<SyncEntityKind, IReadOnlyList<ISyncable>> locals,
        HashSet<Guid> deletedIds, Guid myId)
    {
        var mySnapshot = new DeviceSnapshot { DeviceId = myId, Tombstones = deletedIds.ToList() };
        foreach (var kind in kinds)
            kind.IntoSnapshot(mySnapshot, Live(locals[kind.Kind], deletedIds));
        await _backend.WriteAsync(DevicesKind, myId, _integrity.Wrap(_serializer.Serialize(mySnapshot)));
    }

    private readonly record struct MergeTotals(int Pulled, int Skipped, long Observed);

    private async Task<MergeTotals> MergeRemotesAsync(
        IReadOnlyList<SyncKind> kinds, IReadOnlyList<DeviceSnapshot> remoteSnapshots,
        Dictionary<SyncEntityKind, IReadOnlyList<ISyncable>> locals, HashSet<Guid> deletedIds, long clock)
    {
        var pulled = 0;
        var skipped = 0;
        var observed = clock;
        foreach (var kind in kinds)
        {
            var remoteCandidates = remoteSnapshots.SelectMany(kind.FromSnapshot);
            var outcome = await MergeKindAsync(remoteCandidates, locals[kind.Kind], deletedIds, kind.Apply);
            pulled += outcome.Pulled;
            skipped += outcome.Skipped;
            observed = Math.Max(observed, outcome.Observed);
        }

        return new MergeTotals(pulled, skipped, observed);
    }

    private IReadOnlyList<ISyncable> Live(IReadOnlyList<ISyncable> entities, ISet<Guid> deletedIds) =>
        entities.Where(e => !deletedIds.Contains(e.Id)).ToList();

    private readonly record struct PeerFile(Guid Id, string Content);

    private readonly record struct RawPeers(IReadOnlyList<PeerFile> Peers, bool OwnPresent, int Unreadable);

    private readonly record struct RemoteRead(IReadOnlyList<DeviceSnapshot> Snapshots, int Unreadable);

    private async Task<RawPeers> ReadPeerFilesAsync(Guid myId)
    {
        var ids = await _backend.ListAsync(DevicesKind);
        var ownPresent = ids.Contains(myId);
        var peerIds = ids.Where(id => id != myId).ToList();
        var contents = await Task.WhenAll(peerIds.Select(id => _backend.ReadAsync(DevicesKind, id)));

        var peers = new List<PeerFile>();
        var unreadable = 0;
        for (var i = 0; i < peerIds.Count; i++)
            if (contents[i] is { } content)
            {
                peers.Add(new PeerFile(peerIds[i], content));
            }
            else
            {
                unreadable++;
                _logger.Warning("Skipping device snapshot {DeviceId}: the file is listed but could not be read", peerIds[i]);
            }

        return new RawPeers(peers, ownPresent, unreadable);
    }

    private string? TryComputeFingerprint(IReadOnlyList<PeerFile> peers, int tombstoneCount)
    {
        var tokens = new List<string>();
        foreach (var peer in peers)
        {
            var hash = _integrity.HeaderHash(peer.Content);
            if (hash is null) return null;
            tokens.Add($"{peer.Id:N}:{hash}");
        }

        tokens.Sort(StringComparer.Ordinal);
        return string.Join(";", tokens) + "|" + tombstoneCount;
    }

    private RemoteRead DeserializePeers(IReadOnlyList<PeerFile> peers)
    {
        var snapshots = new List<DeviceSnapshot>();
        var unreadable = 0;
        foreach (var peer in peers)
        {
            if (!_integrity.TryUnwrap(peer.Content, out var json))
            {
                unreadable++;
                _logger.Warning("Skipping device snapshot {DeviceId}: checksum mismatch, the file is corrupt or partial", peer.Id);
                continue;
            }
            try
            {
                snapshots.Add(_serializer.Deserialize<DeviceSnapshot>(json));
            }
            catch (Exception ex)
            {
                unreadable++;
                _logger.Error(ex, "Skipping unreadable device snapshot {DeviceId} during sync", peer.Id);
            }
        }

        return new RemoteRead(snapshots, unreadable);
    }

    private readonly record struct MergeOutcome(int Pulled, int Skipped, long Observed);

    private async Task<MergeOutcome> MergeKindAsync(
        IEnumerable<ISyncable> remoteCandidates, IReadOnlyList<ISyncable> locals, ISet<Guid> deletedIds, Func<ISyncable, Task> apply)
    {
        var candidates = remoteCandidates
            .Select(e => new MergeCandidate<ISyncable>(e.Id, new SyncVersion(e.Lamport, e.LastModifiedByDeviceId), e))
            .ToList();
        long observed = 0;
        foreach (var candidate in candidates)
            observed = Math.Max(observed, candidate.Version.Lamport);

        var winners = _merge.ResolveWinners(candidates, deletedIds);
        var localVersions = locals.ToDictionary(l => l.Id, l => new SyncVersion(l.Lamport, l.LastModifiedByDeviceId));

        var pulled = 0;
        var skipped = 0;
        foreach (var winner in winners)
        {
            if (localVersions.TryGetValue(winner.Id, out var local) && winner.Version.CompareTo(local) <= 0)
                continue;

            winner.Payload.MarkPulled();
            try
            {
                await apply(winner.Payload);
                pulled++;
            }
            catch (Exception ex)
            {
                skipped++;
                _logger.Error(ex, "Skipping {Id} that failed to apply locally during sync", winner.Id);
            }
        }

        return new MergeOutcome(pulled, skipped, observed);
    }

    private async Task<int> SyncImagesAsync()
    {
        if (_imageStore is null) return 0;

        var localSet = (await _imageStore.ListKeysAsync()).ToHashSet();
        var remoteSet = (await _backend.ListBlobKeysAsync(ImageKind)).ToHashSet();
        if (localSet.Count == 0 && remoteSet.Count == 0) return 0;

        var referenced = (await _store.GetReferencedImageKeysAsync()).ToHashSet();

        var failed = 0;
        foreach (var key in referenced)
        {
            try
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
                        failed++;
                        _logger.Warning("Referenced image {Key} could not be downloaded; leaving it for the next sync", key);
                        continue;
                    }
                    using var stream = new MemoryStream(bytes);
                    await _imageStore.ImportAsync(key, stream);
                }
            }
            catch (Exception ex)
            {
                failed++;
                _logger.Error(ex, "Skipping image {Key} that failed to transfer during sync", key);
            }
        }

        foreach (var key in localSet.Where(k => !referenced.Contains(k)))
            await _imageStore.DeleteAsync(key);

        return failed;
    }
}
