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

    private const int DefaultTombstoneRetentionDays = 30;

    private readonly ISyncBackend _backend;
    private readonly ISyncStore _store;
    private readonly ISyncSerializer _serializer;
    private readonly ISyncStatus? _syncStatus;
    private readonly IImageStore? _imageStore;
    private readonly IAppLogger _logger;
    private readonly IReadOnlyList<SyncKind> _kinds;
    private readonly IReadOnlyDictionary<SyncEntityKind, SyncKind> _kindsByKind;

    public SyncService(ISyncBackend backend, ISyncStore store, ISyncSerializer serializer, ISyncStatus? syncStatus = null, IImageStore? imageStore = null, IAppLogger? logger = null)
    {
        _backend = backend;
        _store = store;
        _serializer = serializer;
        _syncStatus = syncStatus;
        _imageStore = imageStore;
        _logger = logger ?? new NullAppLogger();
        _kinds = new SyncKindCatalog().Describe(_store, _serializer);
        _kindsByKind = _kinds.ToDictionary(k => k.Kind);
    }

    public async Task<SyncResult> SyncAsync()
    {
        if (!_backend.IsAvailable)
        {
            _logger.Warning("Sync skipped: the configured sync backend is not available");
            return new SyncResult(0, 0, Array.Empty<SyncConflict>());
        }

        _logger.Information("Sync starting");
        var conflicts = new List<SyncConflict>();

        var reconciled = new List<(int pushed, int pulled, int skipped)>(_kinds.Count);
        foreach (var kind in _kinds)
            reconciled.Add(await ReconcileAsync(kind, conflicts));

        var reconcileComplete = conflicts.Count == 0 && reconciled.All(r => r.skipped == 0);
        await SyncImagesAsync(reconcileComplete);

        var retentionDays = Math.Max(1, _syncStatus?.TombstoneRetentionDays ?? DefaultTombstoneRetentionDays);
        var purged = await _store.PurgeTombstonesAsync(DateTime.UtcNow.AddDays(-retentionDays));
        foreach (var tombstone in purged)
            await _backend.DeleteAsync(KindString(tombstone.Kind), tombstone.Id);

        var result = new SyncResult(
            reconciled.Sum(r => r.pushed),
            reconciled.Sum(r => r.pulled),
            conflicts,
            reconciled.Sum(r => r.skipped));

        _logger.Information("Sync complete: pushed={Pushed} pulled={Pulled} skipped={Skipped} conflicts={Conflicts}",
            result.Pushed, result.Pulled, result.Skipped, result.Conflicts.Count);
        return result;
    }

    public async Task ResolveAsync(SyncConflict conflict, bool keepLocal)
    {
        if (keepLocal)
        {
            var nextRevision = Math.Max(conflict.LocalRevision, conflict.RemoteRevision) + 1;
            await _store.MarkSyncedAsync(conflict.Kind, conflict.Id, conflict.RemoteRevision, dirty: true,
                revision: nextRevision);
            return;
        }

        var kind = KindFor(conflict.Kind);
        var content = await _backend.ReadAsync(kind.WireString, conflict.Id);
        if (content is null) return;

        var entity = kind.Deserialize(content);
        entity.MarkPulled();
        await kind.Apply(entity);
    }

    private async Task<(int pushed, int pulled, int skipped)> ReconcileAsync(SyncKind kind, List<SyncConflict> conflicts)
    {
        var locals = await kind.GetLocal();
        var remoteEntries = await _backend.ListAsync(kind.WireString);
        var remoteRevById = remoteEntries.ToDictionary(e => e.Id, e => e.Revision);
        var localById = locals.ToDictionary(l => ((DomainObject)l).Id);

        var pushed = 0;
        var pulled = 0;
        var skipped = 0;

        foreach (var id in remoteRevById.Keys.Union(localById.Keys))
        {
            localById.TryGetValue(id, out var local);
            var hasRemote = remoteRevById.TryGetValue(id, out var remoteRevision);

            var localChanged = local is not null && local.IsDirty;
            var remoteChanged = hasRemote && (local is null || remoteRevision > local.BaseRevision);

            if (localChanged && remoteChanged)
            {
                var remote = await ReadRemoteAsync(kind, id, remoteRevision);
                conflicts.Add(remote is not null
                    ? new SyncConflict(kind.Kind, id, kind.Label(local!), kind.Label(remote), local!.Revision, remoteRevision)
                    : new SyncConflict(kind.Kind, id, kind.Label(local!), kind.Label(local!), local!.Revision, remoteRevision));
            }
            else if (localChanged)
            {
                await _backend.WriteAsync(kind.WireString, id, kind.Serialize(local!), local!.Revision);
                await _store.MarkSyncedAsync(kind.Kind, id, local!.Revision, dirty: false);
                pushed++;
            }
            else if (remoteChanged)
            {
                var remote = await ReadRemoteAsync(kind, id, remoteRevision);
                if (remote is null) { skipped++; continue; }
                if (local is null && remote.IsDeleted) continue;
                remote.MarkPulled();
                try
                {
                    await kind.Apply(remote);
                    pulled++;
                }
                catch (Exception ex)
                {
                    // One entity that won't apply (e.g. a value referencing a field definition this
                    // database doesn't have) must not abort the whole sync; skip it and carry on.
                    _logger.Error(ex, "Skipping {Kind} {Id} that failed to apply locally during sync", kind.WireString, id);
                    skipped++;
                }
            }
        }

        return (pushed, pulled, skipped);
    }

    private async Task<ISyncable?> ReadRemoteAsync(SyncKind kind, Guid id, long revision)
    {
        var content = await _backend.ReadAtRevisionAsync(kind.WireString, id, revision);
        if (content is null) return null;
        try
        {
            return kind.Deserialize(content);
        }
        catch (Exception ex)
        {
            // A document this build can't deserialize (e.g. an unknown field type from a newer client)
            // is skipped so a single bad document can't abort sync of every other entity.
            _logger.Error(ex, "Skipping un-deserializable {Kind} document {Id} during sync", kind.WireString, id);
            return null;
        }
    }

    private async Task SyncImagesAsync(bool reconcileComplete)
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
            else if (!localSet.Contains(key))
            {
                _logger.Warning("Referenced image {Key} is missing from both the local and remote stores", key);
            }
        }

        foreach (var key in localSet.Where(k => !referenced.Contains(k)))
            await _imageStore.DeleteAsync(key);

        if (reconcileComplete)
            foreach (var key in remoteSet.Where(k => !referenced.Contains(k)))
                await _backend.DeleteBlobAsync(ImageKind, key);
    }

    private SyncKind KindFor(SyncEntityKind kind) =>
        _kindsByKind.TryGetValue(kind, out var k)
            ? k
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sync entity kind");

    private string KindString(SyncEntityKind kind) => KindFor(kind).WireString;
}
