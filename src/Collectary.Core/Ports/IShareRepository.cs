using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface IShareRepository
{
    Task AddOrUpdateAsync(CollectionShare share);
    Task RemoveAsync(Guid presetId, Guid sharedWithUserId);
    Task RemoveAllForPresetAsync(Guid presetId);
    Task<IReadOnlyList<CollectionShare>> GetByPresetAsync(Guid presetId);
    Task<IReadOnlyList<CollectionShare>> GetForUserAsync(Guid userId);
    Task<CollectionShare?> GetAsync(Guid presetId, Guid sharedWithUserId);
}
