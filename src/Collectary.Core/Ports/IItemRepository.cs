using System.Linq.Expressions;
using Collectary.Core.Domain;

namespace Collectary.Core.Ports;

public interface IItemRepository
{
    Task<IReadOnlyList<Item>> GetByPresetAsync(Guid presetId);
    Task<IReadOnlyList<Item>> SearchAsync(Expression<Func<Item, bool>>? serverFilter);
    Task<IReadOnlyCollection<int>> GetUsedAutoNumbersAsync(Guid fieldDefinitionId, Guid? excludeItemId);
    Task<Item?> GetByIdAsync(Guid id);
    Task AddAsync(Item item);
    Task UpdateAsync(Item item);
    Task DeleteAsync(Guid id);
    Task DeleteByPresetAsync(Guid presetId);
}
