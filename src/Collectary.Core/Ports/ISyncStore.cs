using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface ISyncStore
{
    Task<IReadOnlyList<Preset>> GetAllPresetsAsync();
    Task<IReadOnlyList<Item>> GetAllItemsAsync();
    Task<IReadOnlyList<SharedField>> GetAllSharedFieldsAsync();
    Task<IReadOnlyList<User>> GetAllUsersAsync();
    Task<IReadOnlyList<CollectionShare>> GetAllSharesAsync();
    Task ApplyPresetAsync(Preset preset);
    Task ApplyItemAsync(Item item);
    Task ApplySharedFieldAsync(SharedField sharedField);
    Task ApplyUserAsync(User user);
    Task ApplyShareAsync(CollectionShare share);

    Task<IReadOnlyList<Guid>> GetTombstoneIdsAsync();
    Task ApplyDeletionsAsync(IReadOnlyCollection<Guid> ids);
    Task StampPushedAsync(SyncEntityKind kind, Guid id, long lamport, Guid deviceId);
    Task<long> GetMaxObservedLamportAsync();
    Task SetMaxObservedLamportAsync(long value);

    Task<IReadOnlyList<string>> GetReferencedImageKeysAsync();
    Task DeleteLocallyAsync(SyncEntityKind kind, Guid id);
}
