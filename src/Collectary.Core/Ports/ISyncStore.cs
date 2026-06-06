using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface ISyncStore
{
    Task<IReadOnlyList<Preset>> GetAllPresetsAsync();
    Task<IReadOnlyList<Item>> GetAllItemsAsync();
    Task<IReadOnlyList<SharedField>> GetAllSharedFieldsAsync();
    Task ApplyPresetAsync(Preset preset);
    Task ApplyItemAsync(Item item);
    Task ApplySharedFieldAsync(SharedField sharedField);
    Task MarkSyncedAsync(SyncEntityKind kind, Guid id, long baseRevision, bool dirty, long? revision = null);
    Task<IReadOnlyList<PurgedTombstone>> PurgeTombstonesAsync(DateTime cutoff);
    Task<IReadOnlyList<string>> GetReferencedImageKeysAsync();
    Task<IReadOnlyList<string>> GetLiveReferencedImageKeysAsync();
    Task DeleteLocallyAsync(SyncEntityKind kind, Guid id);
}
