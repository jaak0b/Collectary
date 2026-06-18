using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface IItemUseCase
{
    Task<IReadOnlyList<Item>> GetItemsForPresetAsync(Guid presetId);
    Task<Item?> GetItemAsync(Guid id);
    Task CreateItemAsync(Item item);
    Task UpdateItemAsync(Item item);
    Task DeleteItemAsync(Guid id);
}
