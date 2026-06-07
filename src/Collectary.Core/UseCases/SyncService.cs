using Collectary.Core.Domain;
using Collectary.Core.Logging;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class SyncService : ISyncService
{
    public const string PresetKind = "presets";
    public const string ItemKind = "items";
    public const string SharedFieldKind = "sharedfields";
    public const string ImageKind = "images";

    private const int DefaultTombstoneRetentionDays = 30;

    private readonly ISyncBackend _backend;
    private readonly ISyncStore _store;
    private readonly ISyncSerializer _serializer;
    private readonly ISyncStatus? _syncStatus;
    private readonly IImageStore? _imageStore;
    private readonly IAppLogger _logger;

    public SyncService(ISyncBackend backend, ISyncStore store, ISyncSerializer serializer, ISyncStatus? syncStatus = null, IImageStore? imageStore = null, IAppLogger? logger = null)
    {
        _backend = backend;
        _store = store;
        _serializer = serializer;
        _syncStatus = syncStatus;
        _imageStore = imageStore;
        _logger = logger ?? new NullAppLogger();
    }

    public async Task<SyncResult> SyncAsync()
    {
        if (!_backend.IsAvailable)
            return new SyncResult(0, 0, Array.Empty<SyncConflict>());

        var conflicts = new List<SyncConflict>();

        var sharedFields = await ReconcileAsync(
            SharedFieldKind, SyncEntityKind.SharedField,
            await _store.GetAllSharedFieldsAsync(),
            sf => sf.Name,
            sf => _store.ApplySharedFieldAsync(sf),
            conflicts);

        var presets = await ReconcileAsync(
            PresetKind, SyncEntityKind.Preset,
            await _store.GetAllPresetsAsync(),
            p => p.Name,
            p => _store.ApplyPresetAsync(p),
            conflicts);

        var items = await ReconcileAsync(
            ItemKind, SyncEntityKind.Item,
            await _store.GetAllItemsAsync(),
            i => i.DisplayName,
            i => _store.ApplyItemAsync(i),
            conflicts);

        var reconcileComplete = conflicts.Count == 0
            && sharedFields.skipped == 0 && presets.skipped == 0 && items.skipped == 0;
        await SyncImagesAsync(reconcileComplete);

        var retentionDays = Math.Max(1, _syncStatus?.TombstoneRetentionDays ?? DefaultTombstoneRetentionDays);
        var purged = await _store.PurgeTombstonesAsync(DateTime.UtcNow.AddDays(-retentionDays));
        foreach (var tombstone in purged)
            await _backend.DeleteAsync(KindString(tombstone.Kind), tombstone.Id);

        return new SyncResult(
            sharedFields.pushed + presets.pushed + items.pushed,
            sharedFields.pulled + presets.pulled + items.pulled,
            conflicts);
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

        var content = await _backend.ReadAsync(KindString(conflict.Kind), conflict.Id);
        if (content is null) return;

        switch (conflict.Kind)
        {
            case SyncEntityKind.Preset:
                await _store.ApplyPresetAsync(Pull<Preset>(content));
                break;
            case SyncEntityKind.Item:
                await _store.ApplyItemAsync(Pull<Item>(content));
                break;
            default:
                await _store.ApplySharedFieldAsync(Pull<SharedField>(content));
                break;
        }
    }

    private async Task<(int pushed, int pulled, int skipped)> ReconcileAsync<T>(
        string kind,
        SyncEntityKind entityKind,
        IReadOnlyList<T> locals,
        Func<T, string> label,
        Func<T, Task> applyLocal,
        List<SyncConflict> conflicts)
        where T : DomainObject, ISyncable
    {
        var remoteEntries = await _backend.ListAsync(kind);
        var remoteRevById = remoteEntries.ToDictionary(e => e.Id, e => e.Revision);
        var localById = locals.ToDictionary(l => l.Id);

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
                var remote = await ReadRemoteAsync<T>(kind, id, remoteRevision);
                conflicts.Add(remote is not null
                    ? new SyncConflict(entityKind, id, label(local!), label(remote), local!.Revision, remoteRevision)
                    : new SyncConflict(entityKind, id, label(local!), label(local!), local!.Revision, remoteRevision));
            }
            else if (localChanged)
            {
                await _backend.WriteAsync(kind, id, _serializer.Serialize(local!), local!.Revision);
                await _store.MarkSyncedAsync(entityKind, id, local!.Revision, dirty: false);
                pushed++;
            }
            else if (remoteChanged)
            {
                var remote = await ReadRemoteAsync<T>(kind, id, remoteRevision);
                if (remote is null) { skipped++; continue; }
                if (local is null && remote.IsDeleted) continue;
                remote.MarkPulled();
                await applyLocal(remote);
                pulled++;
            }
        }

        return (pushed, pulled, skipped);
    }

    private async Task<T?> ReadRemoteAsync<T>(string kind, Guid id, long revision) where T : class, ISyncable
    {
        var content = await _backend.ReadAtRevisionAsync(kind, id, revision);
        if (content is null) return null;
        try
        {
            return _serializer.Deserialize<T>(content);
        }
        catch (Exception ex)
        {
            // A document this build can't deserialize (e.g. an unknown field type from a newer client)
            // is skipped so a single bad document can't abort sync of every other entity.
            _logger.Error(ex, "Skipping un-deserializable {Kind} document {Id} during sync", kind, id);
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

    private string KindString(SyncEntityKind kind) => kind switch
    {
        SyncEntityKind.Preset => PresetKind,
        SyncEntityKind.Item => ItemKind,
        SyncEntityKind.SharedField => SharedFieldKind,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sync entity kind"),
    };

    private T Pull<T>(string content) where T : ISyncable
    {
        var entity = _serializer.Deserialize<T>(content);
        entity.MarkPulled();
        return entity;
    }
}
