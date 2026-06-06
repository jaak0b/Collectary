using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases;

public class SyncService : ISyncService
{
    public const string PresetKind = "presets";
    public const string ItemKind = "items";
    public const string SharedFieldKind = "sharedfields";
    public const string ImageKind = "images";

    private readonly ISyncBackend _backend;
    private readonly ISyncStore _store;
    private readonly ISyncSerializer _serializer;
    private readonly ISyncStatus? _syncStatus;
    private readonly IImageStore? _imageStore;

    public SyncService(ISyncBackend backend, ISyncStore store, ISyncSerializer serializer, ISyncStatus? syncStatus = null, IImageStore? imageStore = null)
    {
        _backend = backend;
        _store = store;
        _serializer = serializer;
        _syncStatus = syncStatus;
        _imageStore = imageStore;
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

        await SyncImagesAsync();

        if (_syncStatus is not null)
        {
            var retentionDays = Math.Max(1, _syncStatus.TombstoneRetentionDays);
            var purged = await _store.PurgeTombstonesAsync(DateTime.UtcNow.AddDays(-retentionDays));
            foreach (var tombstone in purged)
                await _backend.DeleteAsync(KindString(tombstone.Kind), tombstone.Id);
        }

        return new SyncResult(
            sharedFields.pushed + presets.pushed + items.pushed,
            sharedFields.pulled + presets.pulled + items.pulled,
            conflicts);
    }

    public async Task ResolveAsync(SyncConflict conflict, bool keepLocal)
    {
        if (keepLocal)
        {
            await _store.MarkSyncedAsync(conflict.Kind, conflict.Id, conflict.RemoteRevision, dirty: true,
                revision: conflict.RemoteRevision + 1);
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

    private async Task<(int pushed, int pulled)> ReconcileAsync<T>(
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
        var remoteHasData = remoteEntries.Count > 0;

        var pushed = 0;
        var pulled = 0;

        foreach (var id in remoteRevById.Keys.Union(localById.Keys))
        {
            localById.TryGetValue(id, out var local);
            var hasRemote = remoteRevById.TryGetValue(id, out var remoteRevision);

            if (local is not null && !hasRemote
                && !local.IsDirty && local.BaseRevision > 0 && remoteHasData)
            {
                await _store.DeleteLocallyAsync(entityKind, id);
                pulled++;
                continue;
            }

            var localChanged = local is not null && local.IsDirty;
            var remoteChanged = hasRemote && (local is null || remoteRevision != local.BaseRevision);

            if (localChanged && remoteChanged)
            {
                var remote = _serializer.Deserialize<T>((await _backend.ReadAsync(kind, id))!);
                conflicts.Add(new SyncConflict(entityKind, id, label(local!), label(remote), local!.Revision, remote.Revision));
            }
            else if (localChanged)
            {
                await _backend.WriteAsync(kind, id, _serializer.Serialize(local!), local!.Revision);
                await _store.MarkSyncedAsync(entityKind, id, local!.Revision, dirty: false);
                pushed++;
            }
            else if (remoteChanged)
            {
                var remote = _serializer.Deserialize<T>((await _backend.ReadAsync(kind, id))!);
                remote.MarkPulled();
                await applyLocal(remote);
                pulled++;
            }
        }

        return (pushed, pulled);
    }

    private async Task SyncImagesAsync()
    {
        if (_imageStore is null) return;

        var referenced = (await _store.GetLiveReferencedImageKeysAsync()).ToHashSet();
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
                if (bytes is null) continue;
                using var stream = new MemoryStream(bytes);
                await _imageStore.ImportAsync(key, stream);
            }
        }

        foreach (var key in localSet.Where(k => !referenced.Contains(k)))
            await _imageStore.DeleteAsync(key);
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
